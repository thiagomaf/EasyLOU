﻿using CoreUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LOU
{
    public class Worker : MonoBehaviour
    {
        private const bool VERBOSE_DEBUG = false;
        private bool Intercepting = false;

        private int ProcessId = -1;

        private String ClientStatusMemoryMapMutexName;
        private String ClientStatusMemoryMapName;
        private Int32 ClientStatusMemoryMapSize;
        private MemoryMap ClientStatusMemoryMap;

        private long LastClientCommandTimestamp;
        private int ClientCommandId = 0;
        private String ClientCommandsMemoryMapMutexName;
        private String ClientCommandsMemoryMapName;
        private Int32 ClientCommandsMemoryMapSize;
        private MemoryMap ClientCommandsMemoryMap;

        private ApplicationController applicationController;
        private InputController inputController;
        private LocalPlayer player;
        private Dictionary<String, DynamicObject> FindItemResults;
        private Dictionary<String, ClientObject> FindPermanentResults;
        private Dictionary<String, FloatingPanel> FindPanelResults;
        private Dictionary<String, GameObject> FindGameObjectResults;
        private List<MobileInstance> FindMobileResults;
        private List<MobileInstance> NearbyMonsters;

        private int mainCameraCullingMask;

        private float ScanJournalTime;
        private string ScanJournalMessage;

        private bool leftMouseDown;
        private bool rightMouseDown;

        private Vector3 lastMouseClickPosition;
        private ClientObject lastMouseClickClientObject;

        public void Start()
        {
            Utils.Log("EasyLOU - " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " - LOU.dll started!");

            this.ProcessId = Process.GetCurrentProcess().Id;
            Utils.Log("ProcessId: " + this.ProcessId.ToString());

            this.ClientStatusMemoryMapMutexName = "ELOU_CS_MX_" + this.ProcessId.ToString();
            this.ClientStatusMemoryMapName = "ELOU_CS_" + this.ProcessId.ToString();
            this.ClientStatusMemoryMapSize = 1024 * 1024 * 10;
            this.ClientStatusMemoryMap = new MemoryMap(this.ClientStatusMemoryMapName, this.ClientStatusMemoryMapSize, this.ClientStatusMemoryMapMutexName);

            this.ClientCommandsMemoryMapMutexName = "ELOU_CC_MX_" + this.ProcessId.ToString();
            this.ClientCommandsMemoryMapName = "ELOU_CC_" + this.ProcessId.ToString();
            this.ClientCommandsMemoryMapSize = 1024 * 1024;
            this.ClientCommandsMemoryMap = new MemoryMap(this.ClientCommandsMemoryMapName, this.ClientCommandsMemoryMapSize, this.ClientCommandsMemoryMapMutexName);
        }

        public void OnDestroy()
        {
            Utils.Log("OnDestroy!");
            this.ClientStatusMemoryMap = null;
            this.ClientCommandsMemoryMap = null;
            this.applicationController = null;
            this.inputController = null;
            this.player = null;
            this.FindItemResults = null;
            this.FindPermanentResults = null;
            this.FindPanelResults = null;
            this.FindGameObjectResults = null;
            this.FindMobileResults = null;
            this.lastMouseClickClientObject = null;
        }

        private String ExtractParam(Dictionary<String, String> Params, int Index)
        {
            if (Index >= 0 && Index <= (Params.Count - 1))
            {
                return Params.Values.ElementAt(Index);
            }
            else
            {
                return null;
            }
        }

        private void ProcessClientCommand(ClientCommand ClientCommand)
        {
            if (ClientCommand != null && ClientCommand.TimeStamp != LastClientCommandTimestamp && ClientCommand.CommandType != CommandType.NOP)
            {
                LastClientCommandTimestamp = ClientCommand.TimeStamp;
                Utils.Log("New command " + ClientCommand.CommandType.ToString() + " received at " + LastClientCommandTimestamp.ToString() + "! Params:");
                Utils.Log(string.Join(" ", ClientCommand.CommandParams));
                switch (ClientCommand.CommandType)
                {
                    case CommandType.FindItem:
                        {
                            var watch = new System.Diagnostics.Stopwatch();
                            watch.Start();
                            this.FindItemResults = new Dictionary<string, DynamicObject>();

                            // Try by ObjectId
                            string _objectId = ExtractParam(ClientCommand.CommandParams, 0);
                            if (_objectId != null && ulong.TryParse(_objectId, out ulong objectId))
                            {
                                DynamicObject dynamicObject = Utils.FindDynamicObject(objectId);
                                if (dynamicObject != null)
                                {
                                    this.FindItemResults.Add(objectId.ToString(), dynamicObject);
                                    break;
                                }
                            }

                            // Try by Name (and ContainerId if required)
                            string objectName = ExtractParam(ClientCommand.CommandParams, 0);
                            string _containerId = ExtractParam(ClientCommand.CommandParams, 1);
                            ulong containerId;

                            if (_containerId != null && _containerId != "" && ulong.TryParse(_containerId, out containerId))
                            {
                                this.FindItemResults = Utils.FindDynamicObjectsByName(objectName, containerId);
                            }
                            else
                            {
                                this.FindItemResults = Utils.FindDynamicObjectsByName(objectName);
                            }

                            watch.Stop();
                            Utils.Log("FindItem took " + watch.ElapsedMilliseconds.ToString() + "ms");
                            break;
                        }

                    case CommandType.FindPermanent:
                        {
                            var watch = new System.Diagnostics.Stopwatch();
                            watch.Start();
                            this.FindPermanentResults = new Dictionary<string, ClientObject>();

                            // Try by PermanentId
                            string _permanentId = ExtractParam(ClientCommand.CommandParams, 0);
                            if (_permanentId != null && int.TryParse(_permanentId, out int permanentId))
                            {
                                ClientObject clientObject = Utils.FindPermanentObject(permanentId);
                                if (clientObject != null)
                                {
                                    this.FindPermanentResults.Add(permanentId.ToString(), clientObject);
                                    break;
                                }
                            }

                            // Try by Name and distance (if required)
                            string permanentName = ExtractParam(ClientCommand.CommandParams, 0);
                            string _distance = ExtractParam(ClientCommand.CommandParams, 1);
                            if (_distance != null && _distance != "" && float.TryParse(_distance, out float distance))
                            {
                                this.FindPermanentResults = Utils.FindPermanentObjectByName(permanentName, distance);
                            }
                            else
                            {
                                this.FindPermanentResults = Utils.FindPermanentObjectByName(permanentName);
                            }

                            watch.Stop();
                            Utils.Log("FindPermanent took " + watch.ElapsedMilliseconds.ToString() + "ms");
                            break;
                        }

                    case CommandType.FindPanel:
                        {
                            var watch = new System.Diagnostics.Stopwatch();
                            watch.Start();
                            this.FindPanelResults = new Dictionary<string, FloatingPanel>();

                            string _panelName = ExtractParam(ClientCommand.CommandParams, 0);
                            FloatingPanelManager fpm = FloatingPanelManager.DJCGIMIDOPB;
                            if (fpm != null)
                            {
                                List<FloatingPanel> AGLMPFPPEDK = (List<FloatingPanel>)Utils.GetInstanceField(fpm, "AGLMPFPPEDK");
                                if (AGLMPFPPEDK != null)
                                {
                                    foreach (FloatingPanel floatingPanel in AGLMPFPPEDK)
                                    {
                                        Utils.Log("Panel " + floatingPanel.PanelId);
                                        if (_panelName == null || _panelName == "" || floatingPanel.PanelId.Contains(_panelName))
                                        {
                                            Utils.Log("Panel " + floatingPanel.PanelId + " matches!");
                                            this.FindPanelResults.Add(floatingPanel.PanelId, floatingPanel);
                                            if (this.FindPanelResults.Count == 20)
                                            {
                                                Utils.Log("Breaking at 20, too many.");
                                                break;
                                            }
                                        }

                                    }
                                }
                            }
                            watch.Stop();
                            Utils.Log("FindPanel took " + watch.ElapsedMilliseconds.ToString() + "ms");
                            break;
                        }

                    case CommandType.Key:
                        {
                            string keyCode = ExtractParam(ClientCommand.CommandParams, 0);

                            foreach (InputController.JFNFCCGHCLJ KeyMapping in this.inputController.KeyMappings)
                            {
                                //new InputController.JFNFCCGHCLJ
                                //{
                                //    PCFPPFOLBEM = KeyCode.Space,
                                //    JEJHPLPCHNB = new PFJDGPGOOBP(),
                                //    MKNOODPFNEL = "ToggleCombatModeAction"
                                //}
                                if (KeyMapping.PCFPPFOLBEM == (KeyCode)Enum.Parse(typeof(KeyCode), keyCode))
                                {
                                    KeyMapping.JEJHPLPCHNB.Execute();
                                }
                            }
                            foreach (InputController.JFNFCCGHCLJ KeyMapping in this.inputController.GodKeyMappings)
                            {
                                if (KeyMapping.PCFPPFOLBEM == (KeyCode)Enum.Parse(typeof(KeyCode), keyCode))
                                {
                                    KeyMapping.JEJHPLPCHNB.Execute();
                                }
                            }

                        }
                        break;

                    case CommandType.Move:
                        {
                            string _x = ExtractParam(ClientCommand.CommandParams, 0);
                            float x;
                            string _y = ExtractParam(ClientCommand.CommandParams, 1);
                            float y;
                            string _z = ExtractParam(ClientCommand.CommandParams, 2);
                            float z;
                            if (float.TryParse(_x, out x) && float.TryParse(_y, out y) && float.TryParse(_z, out z))
                            {
                                Utils.Log("Moving to x=" + x + " y=" + y + " z=" + z);
                                Vector3 location = new Vector3(x, y, z);
                                this.player.SetPathLocation(location, false);
                                return;
                            }
                            string _objectId = ExtractParam(ClientCommand.CommandParams, 0);
                            if (ulong.TryParse(_objectId, out ulong objectId))
                            {
                                Utils.Log("Moving to objectId=" + objectId.ToString());
                                ClientObject clientObject = Utils.FindClientObject(objectId);
                                if (clientObject != null)
                                {
                                    this.player.SetPathObject(clientObject, LocalPlayer.FHAIDCMBMHC.None);
                                }
                                return;
                            }
                        }
                        break;

                    case CommandType.Stop:
                        {
                            this.player.StopPathing(0);
                        }
                        break;

                    case CommandType.ScanJournal:
                        {
                            if (this.applicationController?.GameUI?.ChatWindow != null)
                            {
                                string _timeStamp = ExtractParam(ClientCommand.CommandParams, 0);
                                if (_timeStamp != null && _timeStamp != "" && float.TryParse(_timeStamp, out float timeStamp))
                                {
                                    // Obfuscation guessed from GameUI.ChatWindow.SystemMessage()
                                    List<ChatWindow.AEDJOHFMLDG> MEMFCHFEKPN = (List<ChatWindow.AEDJOHFMLDG>)Utils.GetInstanceField(this.applicationController.GameUI.ChatWindow, "MEMFCHFEKPN");
                                    if (MEMFCHFEKPN != null)
                                    {
                                        ChatWindow.AEDJOHFMLDG message = MEMFCHFEKPN.FindLast(m => m.KCFOFGNMOBE >= timeStamp + 0.001f);
                                        if (message != null)
                                        {
                                            this.ScanJournalMessage = message.PIIMGFNGPEI;
                                            this.ScanJournalTime = message.KCFOFGNMOBE;
                                        }
                                        else
                                        {
                                            this.ScanJournalMessage = "N/A";
                                            this.ScanJournalTime = 0;
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    case CommandType.Macro:
                        {
                            string _macro = ExtractParam(ClientCommand.CommandParams, 0);
                            int macro;

                            if (_macro != null && _macro != "" && int.TryParse(_macro, out macro))
                            {
                                KLKFNLBFIAK Macro = new KLKFNLBFIAK(macro);
                                Macro.Execute();
                            }
                        }
                        break;

                    case CommandType.Say:
                        {
                            string _text = ExtractParam(ClientCommand.CommandParams, 0);
                            string text = "/say " + _text;
                            this.applicationController.GPLIHPHPNKL.SendChat(text);
                        }
                        break;

                    case CommandType.SayCustom:
                        {
                            string _text = ExtractParam(ClientCommand.CommandParams, 0);
                            string text = _text;
                            this.applicationController.GPLIHPHPNKL.SendChat(text);
                        }
                        break;

                    case CommandType.ToggleWarPeace:
                        {
                            PFJDGPGOOBP Toggle = new PFJDGPGOOBP();
                            Toggle.Execute();
                        }
                        break;

                    case CommandType.TargetDynamic:
                        {
                            string _objectId = ExtractParam(ClientCommand.CommandParams, 0);
                            ulong objectId;

                            if (_objectId != null && _objectId != "" && ulong.TryParse(_objectId, out objectId))
                            {
                                ClientObject clientObject = Utils.FindClientObject(objectId);
                                if (clientObject != null)
                                {
                                    this.inputController.HandleTargetResponse(clientObject);
                                }
                            }
                        }
                        break;

                    case CommandType.TargetPermanent:
                        {
                            string _permanentId = ExtractParam(ClientCommand.CommandParams, 0);
                            int permanentId;

                            if (_permanentId != null && _permanentId != "" && int.TryParse(_permanentId, out permanentId))
                            {
                                ClientObject clientObject = Utils.FindPermanentObject(permanentId);
                                if (clientObject != null)
                                {
                                    this.inputController.HandleTargetResponse(clientObject);
                                }
                            }
                        }
                        break;

                    case CommandType.TargetLoc:
                        {
                            string _convert = ExtractParam(ClientCommand.CommandParams, 0);
                            string _x = ExtractParam(ClientCommand.CommandParams, 1);
                            string _y = ExtractParam(ClientCommand.CommandParams, 2);
                            string _z = ExtractParam(ClientCommand.CommandParams, 3);
                            string _objectId = ExtractParam(ClientCommand.CommandParams, 4);

                            if (bool.TryParse(_convert, out bool convert) &&
                                float.TryParse(_x, out float x) &&
                                float.TryParse(_y, out float y) &&
                                float.TryParse(_z, out float z))
                            {
                                Vector3 loc = new Vector3(x, y, z);
                                if (ulong.TryParse(_objectId, out ulong objectId))
                                {
                                    ClientObject clientObject = Utils.FindClientObject(objectId);
                                    if (clientObject != null)
                                    {
                                        this.inputController.HandleTargetLocResponse(loc, clientObject);
                                    }
                                    else
                                    {
                                        this.inputController.HandleTargetLocResponse(loc, null);
                                    }
                                }
                                else
                                {
                                    this.inputController.HandleTargetLocResponse(loc, null);
                                }

                            }
                        }
                        break;

                    case CommandType.LastTarget:
                        {
                            this.inputController.TargetLast();
                        }
                        break;

                    case CommandType.TargetSelf:
                        {
                            this.inputController.TargetSelf();
                        }
                        break;

                    case CommandType.AttackSelected:
                        {
                            string _objectId = ExtractParam(ClientCommand.CommandParams, 0);
                            ulong objectId;
                            if (_objectId != null && _objectId != "" && ulong.TryParse(_objectId, out objectId))
                            {
                                GameObjectSingleton<ApplicationController>.DJCGIMIDOPB.GPLIHPHPNKL.SendScriptCommand(string.Concat(new object[]
                                {
                                    "use ",
                                    objectId,
                                    " ",
                                    "Attack"
                                }), 0UL);
                            }
                        }
                        break;
                    case CommandType.UseSelected:
                        {
                            string _objectId = ExtractParam(ClientCommand.CommandParams, 0);
                            ulong objectId;
                            if (_objectId != null && _objectId != "" && ulong.TryParse(_objectId, out objectId))
                            {
                                DynamicObject dynamicObject = Utils.FindDynamicObject(objectId, 0);
                                if (dynamicObject != null)
                                {
                                    dynamicObject.DoDoubleClickAction();
                                }
                            }
                        }
                        break;

                    case CommandType.Drag:
                        {
                            string _objectId = ExtractParam(ClientCommand.CommandParams, 0);
                            string _containerId = ExtractParam(ClientCommand.CommandParams, 1);
                            ulong objectId;
                            ulong containerId;
                            if (_objectId != null && _objectId != "" && ulong.TryParse(_objectId, out objectId))
                            {
                                ClientObject clientObject = null;
                                if (_containerId != null && _containerId != "" && ulong.TryParse(_containerId, out containerId))
                                {
                                    clientObject = Utils.FindClientObject(objectId, containerId);
                                }
                                else
                                {
                                    clientObject = Utils.FindClientObject(objectId);
                                }
                                if (clientObject != null)
                                {
                                    DynamicObject dynamicObject = clientObject.DynamicInst;
                                    if (dynamicObject != null)
                                    {
                                        player.DoPickup(dynamicObject);
                                    }
                                }
                            }
                        }
                        break;

                    case CommandType.Dropc:
                        {
                            string container = ExtractParam(ClientCommand.CommandParams, 0);

                            DynamicObject carriedObject = ClientObjectManager.DJCGIMIDOPB.GetCarriedObject();
                            if (carriedObject != null)
                            {
                                this.applicationController.GPLIHPHPNKL.SendRequestDrop(carriedObject.ObjectId, null, ulong.Parse(container), false);
                            }
                        }
                        break;

                    case CommandType.Dropg:
                        {
                            float x = float.Parse(ExtractParam(ClientCommand.CommandParams, 0));
                            float y = float.Parse(ExtractParam(ClientCommand.CommandParams, 1));
                            float z = float.Parse(ExtractParam(ClientCommand.CommandParams, 2));

                            DynamicObject carriedObject = ClientObjectManager.DJCGIMIDOPB.GetCarriedObject();
                            if (carriedObject != null)
                            {
                                this.applicationController.GPLIHPHPNKL.SendRequestDrop(carriedObject.ObjectId, new Vector3(x, y, z), 0UL, false);
                            }
                        }
                        break;

                    case CommandType.FindGameObject:
                        {
                            this.FindGameObjectResults = new Dictionary<string, GameObject>();

                            // Try by Name (and ContainerId if required)
                            string objectName = ExtractParam(ClientCommand.CommandParams, 0);
                            this.FindGameObjectResults = Utils.FindGameObjectsByName(objectName);

                            break;
                        }

                    case CommandType.FindMobile:
                        {
                            this.FindMobileResults = new List<MobileInstance>();

                            // Try by ObjectId
                            string _objectId = ExtractParam(ClientCommand.CommandParams, 0);
                            if (_objectId != null && ulong.TryParse(_objectId, out ulong objectId))
                            {
                                Utils.Log("Trying by object id");
                                MobileInstance mobile = Utils.GetMobile(objectId);
                                if (mobile != null)
                                {
                                    this.FindMobileResults.Add(mobile);
                                    break;
                                }
                            }

                            // Try by Name and distance (if required)
                            string name = ExtractParam(ClientCommand.CommandParams, 0);
                            string _distance = ExtractParam(ClientCommand.CommandParams, 1);
                            if (_distance != null && _distance != "" && float.TryParse(_distance, out float distance))
                            {
                                this.FindMobileResults = Utils.FindMobile(name, distance);
                            }
                            else
                            {
                                this.FindMobileResults = Utils.FindMobile(name);
                            }

                            break;
                        }

                    case CommandType.SetUsername:
                        {
                            LoginUI loginUI = UnityEngine.Object.FindObjectOfType<LoginUI>();
                            if (loginUI != null)
                            {
                                string _username = ExtractParam(ClientCommand.CommandParams, 0);
                                loginUI.UsernameTextField.KEJLIDGLCDP = _username;
                            }
                        }
                        break;

                    case CommandType.SetPassword:
                        {
                            LoginUI loginUI = UnityEngine.Object.FindObjectOfType<LoginUI>();
                            if (loginUI != null)
                            {
                                string _password = ExtractParam(ClientCommand.CommandParams, 0);
                                loginUI.PasswordTextField.KEJLIDGLCDP = _password;
                            }
                        }
                        break;

                    case CommandType.Login:
                        {
                            LoginUI loginUI = UnityEngine.Object.FindObjectOfType<LoginUI>();
                            if (loginUI != null)
                            {
                                loginUI.OnMultiplayerClicked();
                            }
                        }
                        break;

                    case CommandType.SelectServer:
                        {
                            LoginUI loginUI = UnityEngine.Object.FindObjectOfType<LoginUI>();
                            if (loginUI != null)
                            {
                                string _server = ExtractParam(ClientCommand.CommandParams, 0);
                                this.applicationController.SendServerSelected(_server);
                            }
                        }
                        break;

                    case CommandType.CharacterSelect:
                        {
                            LoginUI loginUI = UnityEngine.Object.FindObjectOfType<LoginUI>();
                            if (loginUI != null)
                            {
                                string _character = ExtractParam(ClientCommand.CommandParams, 0);
                                if (_character != null && _character != "" && int.TryParse(_character, out int character))
                                {
                                    if (character >= 0 && character <= loginUI.CharacterSelectButtons.Length - 1)
                                    {
                                        string name = loginUI.CharacterSelectButtons[character].transform.name;
                                        GameObjectSingleton<ApplicationController>.DJCGIMIDOPB.GPLIHPHPNKL.SendCharacterSelected(int.Parse(name));
                                    }
                                }
                            }
                        }
                        break;

                    case CommandType.ClickButton:
                        {
                            string _containerName = ExtractParam(ClientCommand.CommandParams, 0);
                            string _buttonName = ExtractParam(ClientCommand.CommandParams, 1);
                            FloatingPanelManager fpm = FloatingPanelManager.DJCGIMIDOPB;
                            if (fpm != null)
                            {
                                FloatingPanel floatingPanel = fpm.GetPanel(_containerName);
                                if (floatingPanel != null)
                                {
                                    DynamicWindow dynamicWindow = floatingPanel.GetComponent<DynamicWindow>();
                                    if (dynamicWindow != null)
                                    {
                                        GameObject objectFound = null;
                                        List<GameObject> Children = new List<GameObject>();
                                        for (var c = 0; c < dynamicWindow.transform.childCount; c++)
                                        {
                                            Transform child = dynamicWindow.transform.GetChild(c);
                                            Children.Add(child.gameObject);
                                            Utils.Log("Child gameobject name: " + child.gameObject.name);
                                            if (child.gameObject.name == _buttonName)
                                            {
                                                Utils.Log("GameObject found!");
                                                objectFound = child.gameObject;
                                            }
                                            Component[] components = child.GetComponents<Component>();
                                            foreach (Component component in components)
                                            {
                                                Utils.Log("Child component: " + component.name + "," + component.GetType().ToString());
                                            }
                                        }
                                        if (objectFound != null)
                                        {
                                            Utils.Log("OnButtonClicked(" + objectFound.name + ")");
                                            dynamicWindow.OnButtonClicked(objectFound);
                                            break;
                                        }
                                    }

                                }
                            }
                        }
                        break;

                    case CommandType.SetInput:
                        {
                            string _containerName = ExtractParam(ClientCommand.CommandParams, 0);
                            string _inputName = ExtractParam(ClientCommand.CommandParams, 1);
                            string _newValue = ExtractParam(ClientCommand.CommandParams, 2);
                            FloatingPanelManager fpm = FloatingPanelManager.DJCGIMIDOPB;
                            if (fpm != null)
                            {
                                FloatingPanel floatingPanel = fpm.GetPanel(_containerName);
                                if (floatingPanel != null)
                                {
                                    DynamicWindow dynamicWindow = floatingPanel.GetComponent<DynamicWindow>();
                                    if (dynamicWindow != null)
                                    {
                                        GameObject objectFound = null;
                                        List<GameObject> Children = new List<GameObject>();
                                        for (var c = 0; c < dynamicWindow.transform.childCount; c++)
                                        {
                                            Transform child = dynamicWindow.transform.GetChild(c);
                                            Children.Add(child.gameObject);
                                            Utils.Log("Child gameobject name: " + child.gameObject.name);
                                            if (child.gameObject.name == _inputName)
                                            {
                                                Utils.Log("GameObject found!");
                                                objectFound = child.gameObject;
                                            }
                                            Component[] components = child.GetComponents<Component>();
                                            foreach (Component component in components)
                                            {
                                                Utils.Log("Child component: " + component.name + "," + component.GetType().ToString());
                                            }
                                        }
                                        if (objectFound != null)
                                        {
                                            Utils.Log("Set(" + _newValue + ")");
                                            DynamicWindowTextField input = objectFound.GetComponent<DynamicWindowTextField>();
                                            input.GOAGGCMCIBB = _newValue;
                                            break;
                                        }
                                    }

                                }
                            }
                        }
                        break;

                    case CommandType.SetTargetFrameRate:
                        {
                            string _targetFrameRate = ExtractParam(ClientCommand.CommandParams, 0);
                            if (_targetFrameRate != null && _targetFrameRate != "" && int.TryParse(_targetFrameRate, out int targetFrameRate))
                            {
                                Application.targetFrameRate = targetFrameRate;
                            }
                        }
                        break;

                    case CommandType.SetVSyncCount:
                        {
                            string _vSyncCount = ExtractParam(ClientCommand.CommandParams, 0);
                            if (_vSyncCount != null && _vSyncCount != "" && int.TryParse(_vSyncCount, out int vSyncCount))
                            {
                                QualitySettings.vSyncCount = vSyncCount;
                            }
                        }
                        break;

                    case CommandType.SetMainCameraMask:
                        {
                            string _cullingMask = ExtractParam(ClientCommand.CommandParams, 0);
                            if (_cullingMask != null && _cullingMask != "" && int.TryParse(_cullingMask, out int cullingMask))
                            {
                                Camera.main.cullingMask = cullingMask;
                            }
                        }
                        break;

                    default:
                        Utils.Log("Not Implemented!");
                        break;
                }
            }
        }


        private void UpdateClientStatus()
        {
            ClientStatus ClientStatus = new ClientStatus();
            ClientStatus.TimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            if (this.player)
            {
                //Utils.Log("Props:");
                //Dictionary<string, object> EBNBHBHNCFC = (Dictionary<string, object>)Utils.GetInstanceField(this.player, "EBNBHBHNCFC");

                ClientStatus.CharacterInfo["CHARNAME"] = this.player.name;
                ClientStatus.CharacterInfo["CHARID"] = this.player != null ? this.player.ObjectId.ToString() : "N/A";
                ClientStatus.CharacterInfo["CHARPOSX"] = this.player.transform != null && this.player.transform.position != null ? this.player.transform.position.x.ToString() : "-1";
                ClientStatus.CharacterInfo["CHARPOSY"] = this.player.transform != null && this.player.transform.position != null ? this.player.transform.position.y.ToString() : "-1";
                ClientStatus.CharacterInfo["CHARPOSZ"] = this.player.transform != null && this.player.transform.position != null ? this.player.transform.position.z.ToString() : "-1";
                ClientStatus.CharacterInfo["CHARDIR"] = this.player.transform != null && this.player.transform.rotation != null ? this.player.transform.rotation.ToString() : "N/A";
                ClientStatus.CharacterInfo["CHARSTATUS"] = "" +
                    (this.player.IsInCombatMode() ? "G" : " ") +
                    (this.player.PLFMFNKLBON == CoreUtil.ShardShared.MobileFrozenState.MoveFrozen || this.player.PLFMFNKLBON == CoreUtil.ShardShared.MobileFrozenState.MoveAndTurnFrozen ? "A" : "");
                ClientStatus.CharacterInfo["CHARGHOST"] = this.player.GetObjectProperty("IsDead") != null ? this.player.GetObjectProperty("IsDead").ToString() : "N/A";
                ClientStatus.CharacterInfo["BACKPACKID"] = this.player.GetEquippedObject("Backpack") != null ? this.player.GetEquippedObject("Backpack").DMCIODGEHCN.ToString() : "N/A";
                ClientStatus.CharacterInfo["BACKPACKWEIGHT"] = this.player.GetEquippedObject("Backpack") != null && this.player.GetEquippedObject("Backpack").GetComponent<DynamicObject>() != null && this.player.GetEquippedObject("Backpack").GetComponent<DynamicObject>().GetObjectProperty("Weight") != null ? this.player.GetEquippedObject("Backpack").GetComponent<DynamicObject>().GetObjectProperty("Weight").ToString() : "N/A";
                ClientStatus.CharacterInfo["HEADID"] = this.player.GetEquippedObject("Head") != null ? this.player.GetEquippedObject("Head").DMCIODGEHCN.ToString() : "N/A";
                ClientStatus.CharacterInfo["HEADNAME"] = this.player.GetEquippedObject("Head") != null ? this.player.GetEquippedObject("Head").name.ToString() : "N/A";
                ClientStatus.CharacterInfo["CHESTID"] = this.player.GetEquippedObject("Chest") != null ? this.player.GetEquippedObject("Chest").DMCIODGEHCN.ToString() : "N/A";
                ClientStatus.CharacterInfo["CHESTNAME"] = this.player.GetEquippedObject("Chest") != null ? this.player.GetEquippedObject("Chest").name.ToString() : "N/A";
                ClientStatus.CharacterInfo["LEGSID"] = this.player.GetEquippedObject("Legs") != null ? this.player.GetEquippedObject("Legs").DMCIODGEHCN.ToString() : "N/A";
                ClientStatus.CharacterInfo["LEGSNAME"] = this.player.GetEquippedObject("Legs") != null ? this.player.GetEquippedObject("Legs").name.ToString() : "N/A";
                ClientStatus.CharacterInfo["RIGHTHANDID"] = this.player.GetEquippedObject("RightHand") != null ? this.player.GetEquippedObject("RightHand").DMCIODGEHCN.ToString() : "N/A";
                ClientStatus.CharacterInfo["RIGHTHANDNAME"] = this.player.GetEquippedObject("RightHand") != null ? this.player.GetEquippedObject("RightHand").name.ToString() : "N/A";
                ClientStatus.CharacterInfo["LEFTHANDID"] = this.player.GetEquippedObject("LeftHand") != null ? this.player.GetEquippedObject("LeftHand").DMCIODGEHCN.ToString() : "N/A";
                ClientStatus.CharacterInfo["LEFTHANDNAME"] = this.player.GetEquippedObject("LeftHand") != null ? this.player.GetEquippedObject("LeftHand").name.ToString() : "N/A";
            }


            if (this.FindItemResults != null && this.FindItemResults.Count > 0)
            {
                try
                {
                    ClientStatus.Find["FINDITEMID"] = String.Join(",", this.FindItemResults.Keys);
                    ClientStatus.Find["FINDITEMNAME"] = String.Join(",", this.FindItemResults.Values.Select(v => v.name));
                    ClientStatus.Find["FINDITEMCNTID"] = String.Join(",", this.FindItemResults.Values.Select(v => v.ContainerId.ToString()));
                }
                catch (Exception e)
                {
                    Utils.Log(e.ToString());
                    this.FindItemResults = new Dictionary<string, DynamicObject>();
                    ClientStatus.Find["FINDITEMID"] = "N/A";
                    ClientStatus.Find["FINDITEMNAME"] = "N/A";
                    ClientStatus.Find["FINDITEMCNTID"] = "N/A";
                }
            }
            else
            {
                ClientStatus.Find["FINDITEMID"] = "N/A";
                ClientStatus.Find["FINDITEMNAME"] = "N/A";
                ClientStatus.Find["FINDITEMCNTID"] = "N/A";
            }

            if (this.FindPermanentResults != null && this.FindPermanentResults.Count > 0)
            {
                try
                {
                    ClientStatus.Find["FINDPERMAID"] = String.Join(",", this.FindPermanentResults.Keys);
                    ClientStatus.Find["FINDPERMANAME"] = String.Join(",", this.FindPermanentResults.Values.Select(v => v.name));
                    ClientStatus.Find["FINDPERMADIST"] = String.Join(",", this.FindPermanentResults.Values.Select(v => Vector3.Distance(v.transform.position, this.player.transform.position)));
                }
                catch (Exception e)
                {
                    Utils.Log(e.ToString());
                    this.FindPermanentResults = new Dictionary<string, ClientObject>();
                    ClientStatus.Find["FINDPERMAID"] = "N/A";
                    ClientStatus.Find["FINDPERMANAME"] = "N/A";
                    ClientStatus.Find["FINDPERMADIST"] = "N/A";
                }
            }
            else
            {
                ClientStatus.Find["FINDPERMAID"] = "N/A";
                ClientStatus.Find["FINDPERMANAME"] = "N/A";
                ClientStatus.Find["FINDPERMADIST"] = "N/A";
            }

            if (this.FindPanelResults != null && this.FindPanelResults.Count > 0)
            {
                try
                {
                    ClientStatus.Find["FINDPANELID"] = String.Join(",", this.FindPanelResults.Keys);
                }
                catch (Exception e)
                {
                    Utils.Log(e.ToString());
                    this.FindPanelResults = new Dictionary<string, FloatingPanel>();
                }
            }
            else
            {
                ClientStatus.Find["FINDPANELID"] = "N/A";
            }

            if (this.FindGameObjectResults != null && this.FindGameObjectResults.Count > 0)
            {
                try
                {
                    ClientStatus.Find["FINDGAMEOBJECTID"] = String.Join(",", this.FindGameObjectResults.Keys);
                }
                catch (Exception e)
                {
                    Utils.Log(e.ToString());
                    this.FindGameObjectResults = new Dictionary<string, GameObject>();
                }
            }
            else
            {
                ClientStatus.Find["FINDGAMEOBJECTID"] = "N/A";
            }
            if (this.FindMobileResults != null && this.FindMobileResults.Count > 0)
            {
                try
                {
                    ClientStatus.Find["FINDMOBILEID"] = String.Join(",", this.FindMobileResults.Select(v => v.ObjectId));
                    ClientStatus.Find["FINDMOBILENAME"] = String.Join(",", this.FindMobileResults.Select(v => v.name));
                    ClientStatus.Find["FINDMOBILEHP"] = String.Join(",", this.FindMobileResults.Select(v => v.GetStatByName("Health")));
                    ClientStatus.Find["FINDMOBILEDIST"] = String.Join(",", this.FindMobileResults.Select(v => Vector3.Distance(v.transform.position, this.player.transform.position)));
                    ClientStatus.Find["FINDMOBILETYPE"] = String.Join(",", this.FindMobileResults.Select(v => v.DKCMJFOPPDL));
                }
                catch (Exception e)
                {
                    ClientStatus.Find["FINDMOBILEID"] = "N/A";
                    ClientStatus.Find["FINDMOBILENAME"] = "N/A";
                    ClientStatus.Find["FINDMOBILEHP"] = "N/A";
                    ClientStatus.Find["FINDMOBILEDIST"] = "N/A";
                    ClientStatus.Find["FINDMOBILETYPE"] = "N/A";
                    Utils.Log(e.ToString());
                    this.FindMobileResults = new List<MobileInstance>();
                }
            }
            else
            {
                ClientStatus.Find["FINDMOBILEID"] = "N/A";
                ClientStatus.Find["FINDMOBILENAME"] = "N/A";
                ClientStatus.Find["FINDMOBILEHP"] = "N/A";
                ClientStatus.Find["FINDMOBILEDIST"] = "N/A";
                ClientStatus.Find["FINDMOBILETYPE"] = "N/A";
            }

            if (inputController != null)
            {
                if (inputController.BODCEBEPNMH != null && inputController.BODCEBEPNMH.AOJMJNFMBJO != null)
                {
                    ClientStatus.LastAction["COBJECTID"] = inputController.BODCEBEPNMH.ObjectId.ToString();
                }
                else
                {
                    ClientStatus.LastAction["COBJECTID"] = "-1";
                }
                if (inputController.MFJFNHLOHOI != null && inputController.MFJFNHLOHOI.AOJMJNFMBJO != null)
                {
                    ClientStatus.LastAction["LOBJECTID"] = inputController.MFJFNHLOHOI.ObjectId.ToString();
                }
                else
                {
                    ClientStatus.LastAction["LOBJECTID"] = "-1";
                }
            }

            ClientStatus.ClientInfo["CLIVER"] = ApplicationController.c_clientVersion ?? "N/A";
            ClientStatus.ClientInfo["CLIID"] = this.ProcessId.ToString();
            ClientStatus.ClientInfo["CLIXRES"] = Screen.width.ToString();
            ClientStatus.ClientInfo["CLIYRES"] = Screen.height.ToString();
            ClientStatus.ClientInfo["FULLSCREEN"] = Screen.fullScreen.ToString();
            ClientStatus.ClientInfo["CLIGAMESTATE"] = this.applicationController != null ? this.applicationController.JOJPMHOLNHA.ToString() : "N/A";
            ClientStatus.ClientInfo["SERVER"] = Utils.GetInstanceField(applicationController, "EGBNKJDFBEJ") != null ? (string)Utils.GetInstanceField(applicationController, "EGBNKJDFBEJ") : "N/A";
            ClientStatus.ClientInfo["TARGETFRAMERATE"] = Application.targetFrameRate.ToString();
            ClientStatus.ClientInfo["VSYNCCOUNT"] = QualitySettings.vSyncCount.ToString();
            ClientStatus.ClientInfo["MAINCAMERAMASK"] = Camera.main.cullingMask.ToString();

            if (this.player != null)
            {
                foreach (KeyValuePair<string, DestructibleInstance.LPNMCMFLAKC> StatValue in this.player.StatValues)
                {
                    ClientStatus.StatusBar[StatValue.Key.ToUpper()] = this.player.GetStatByName(StatValue.Key).ToString();
                }
            }

            if (inputController != null)
            {
                InputController.FBKEBHPKOIC targetType = (InputController.FBKEBHPKOIC)Utils.GetInstanceField(inputController, "BFNLCIMBCJF");
                ClientStatus.Miscellaneous["TARGETTYPE"] = targetType.ToString();
            }

            if (Input.mousePosition != null &&
                Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width &&
                Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height)
            {
                ClientStatus.Miscellaneous["MOUSEWINDOWX"] = Input.mousePosition.x.ToString();
                ClientStatus.Miscellaneous["MOUSEWINDOWY"] = Input.mousePosition.y.ToString();
                ClientStatus.Miscellaneous["MOUSEWINDOWZ"] = Input.mousePosition.z.ToString();

                RaycastHit raycastHit;
                if (Camera.main != null)
                {
                    JKDPNLPCCNI.GetSurfaceHit(Camera.main.ScreenPointToRay(Input.mousePosition), out raycastHit, JKDPNLPCCNI.IMHGKJPOBHP.All);

                    if (raycastHit.point != null)
                    {
                        ClientStatus.Miscellaneous["MOUSEWORLDX"] = raycastHit.point.x.ToString();
                        ClientStatus.Miscellaneous["MOUSEWORLDY"] = raycastHit.point.y.ToString();
                        ClientStatus.Miscellaneous["MOUSEWORLDZ"] = raycastHit.point.z.ToString();
                    }
                }
                else
                {
                    ClientStatus.Miscellaneous["MOUSEWORLDX"] = "N/A";
                    ClientStatus.Miscellaneous["MOUSEWORLDY"] = "N/A";
                    ClientStatus.Miscellaneous["MOUSEWORLDZ"] = "N/A";
                }

                if (this.inputController != null && this.inputController.ObjectPicker != null && this.inputController.ObjectPicker.HFHBOINDMAJ != null)
                {
                    ClientStatus.Miscellaneous["MOUSEOVERPERID"] = this.inputController.ObjectPicker.HFHBOINDMAJ.PermanentId.ToString();
                    ClientStatus.Miscellaneous["MOUSEOVERNAME"] = this.inputController.ObjectPicker.HFHBOINDMAJ.name;
                    ClientStatus.Miscellaneous["MOUSEOVEROBJID"] = this.inputController.ObjectPicker.HFHBOINDMAJ.DynamicInst != null ? this.inputController.ObjectPicker.HFHBOINDMAJ.DynamicInst.ObjectId.ToString() : "null";
                    ClientStatus.Miscellaneous["MOUSEOVEROBJNAME"] = this.inputController.ObjectPicker.HFHBOINDMAJ.DynamicInst != null ? this.inputController.ObjectPicker.HFHBOINDMAJ.DynamicInst.name : "null";
                    ClientStatus.Miscellaneous["MOUSEOVEROBJCNTID"] = this.inputController.ObjectPicker.HFHBOINDMAJ.DynamicInst != null ? this.inputController.ObjectPicker.HFHBOINDMAJ.DynamicInst.ContainerId.ToString() : "null";
                }

                UICamera.Raycast(Input.mousePosition);
                if (UICamera.EHDALGCGPEK != null)
                {
                    ClientStatus.Miscellaneous["MOUSEOVERUINAME"] = UICamera.EHDALGCGPEK.name != null ? UICamera.EHDALGCGPEK.name : "N/A";
                }
                else
                {
                    ClientStatus.Miscellaneous["MOUSEOVERUINAME"] = "N/A";
                }
            }
            else
            {

            }

            if (this.lastMouseClickPosition != null)
            {
                ClientStatus.Miscellaneous["CLICKWINDOWX"] = this.lastMouseClickPosition.x.ToString();
                ClientStatus.Miscellaneous["CLICKWINDOWY"] = this.lastMouseClickPosition.y.ToString();
                ClientStatus.Miscellaneous["CLICKWINDOWZ"] = this.lastMouseClickPosition.z.ToString();
                if (Camera.main != null)
                {

                    RaycastHit raycastHit;
                    JKDPNLPCCNI.GetSurfaceHit(Camera.main.ScreenPointToRay(this.lastMouseClickPosition), out raycastHit, JKDPNLPCCNI.IMHGKJPOBHP.All);
                    if (raycastHit.point != null)
                    {
                        ClientStatus.Miscellaneous["CLICKWORLDX"] = raycastHit.point.x.ToString();
                        ClientStatus.Miscellaneous["CLICKWORLDY"] = raycastHit.point.y.ToString();
                        ClientStatus.Miscellaneous["CLICKWORLDZ"] = raycastHit.point.z.ToString();
                    }
                }
                else
                {
                    ClientStatus.Miscellaneous["CLICKWORLDX"] = "N/A";
                    ClientStatus.Miscellaneous["CLICKWORLDY"] = "N/A";
                    ClientStatus.Miscellaneous["CLICKWORLDZ"] = "N/A";
                }

                if (this.lastMouseClickClientObject != null)
                {
                    ClientStatus.Miscellaneous["CLICKPERID"] = this.lastMouseClickClientObject.PermanentId.ToString();
                    ClientStatus.Miscellaneous["CLICKNAME"] = this.lastMouseClickClientObject.name;
                    ClientStatus.Miscellaneous["CLICKOBJID"] = this.lastMouseClickClientObject.DynamicInst != null ? this.lastMouseClickClientObject.DynamicInst.ObjectId.ToString() : "null";
                    ClientStatus.Miscellaneous["CLICKOBJNAME"] = this.lastMouseClickClientObject.DynamicInst != null ? this.lastMouseClickClientObject.DynamicInst.name : "null";
                    ClientStatus.Miscellaneous["CLICKOBJCNTID"] = this.lastMouseClickClientObject.DynamicInst != null ? this.lastMouseClickClientObject.DynamicInst.ContainerId.ToString() : "null";
                }
            }
            ClientStatus.Miscellaneous["MONSTERSNEARBY"] = "False";
            this.NearbyMonsters = new List<MobileInstance>();
            if (this.player != null)
            {
                IEnumerable<MobileInstance> Nearby = Utils.GetNearbyMobiles(5);
                if (Nearby != null)
                {
                    foreach (var Mobile in Nearby)
                    {
                        if (Mobile.DKCMJFOPPDL == "Monster" && Mobile.BMHLGHANHDL != null && Mobile.BMHLGHANHDL["IsDead"].ToString() == "False")
                        {
                            ClientStatus.Miscellaneous["MONSTERSNEARBY"] = "True";
                            this.NearbyMonsters.Add(Mobile);
                        }
                    }
                }
                if (this.NearbyMonsters.Count > 0)
                {
                    try
                    {
                        ClientStatus.Miscellaneous["MONSTERSID"] = String.Join(",", this.NearbyMonsters.Select(v => v.ObjectId));
                        ClientStatus.Miscellaneous["MONSTERSNAME"] = String.Join(",", this.NearbyMonsters.Select(v => v.name));
                        ClientStatus.Miscellaneous["MONSTERSHP"] = String.Join(",", this.NearbyMonsters.Select(v => v.GetStatByName("Health")));
                        ClientStatus.Miscellaneous["MONSTERSDIST"] = String.Join(",", this.NearbyMonsters.Select(v => Vector3.Distance(v.transform.position, this.player.transform.position)));
                    }
                    catch (Exception e)
                    {
                        Utils.Log(e.ToString());
                        this.NearbyMonsters = new List<MobileInstance>();
                        ClientStatus.Miscellaneous["MONSTERSID"] = "N/A";
                        ClientStatus.Miscellaneous["MONSTERSNAME"] = "N/A";
                        ClientStatus.Miscellaneous["MONSTERSHP"] = "N/A";
                        ClientStatus.Miscellaneous["MONSTERSDIST"] = "N/A";

                    }
                }
                else
                {
                    ClientStatus.Miscellaneous["MONSTERSID"] = "N/A";
                    ClientStatus.Miscellaneous["MONSTERSNAME"] = "N/A";
                    ClientStatus.Miscellaneous["MONSTERSHP"] = "N/A";
                    ClientStatus.Miscellaneous["MONSTERSDIST"] = "N/A";
                }
            }
            else
            {
                ClientStatus.Miscellaneous["MONSTERSID"] = "N/A";
                ClientStatus.Miscellaneous["MONSTERSNAME"] = "N/A";
                ClientStatus.Miscellaneous["MONSTERSHP"] = "N/A";
                ClientStatus.Miscellaneous["MONSTERSDIST"] = "N/A";
            }

            ClientStatus.Miscellaneous["RANDOM"] = new System.Random().Next(0, 1000).ToString();

            ClientStatus.Miscellaneous["TIME"] = Time.time.ToString();

            ClientStatus.Miscellaneous["SCANJOURNALTIME"] = this.ScanJournalTime.ToString();
            ClientStatus.Miscellaneous["SCANJOURNALMESSAGE"] = this.ScanJournalMessage ?? "N/A";

            ClientStatus.Miscellaneous["COMMANDID"] = this.ClientCommandId.ToString();

            //Utils.Log("UpdateStatus!");
            if (this.ProcessId != -1 && ClientStatusMemoryMap != null)
            {
                ClientStatusMemoryMap.WriteMemoryMap<ClientStatus>(ClientStatus);
            }
        }

        private float update;
        void Update()
        {
            //Utils.Log("Update() Start");

            try
            {
                //Utils.Log("DeltaTime = " + Time.deltaTime.ToString());
                update += Time.deltaTime;

                if (
                    Input.mousePosition != null &&
                    Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width &&
                    Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (!this.leftMouseDown)
                        {
                            this.lastMouseClickPosition = Input.mousePosition;
                            this.lastMouseClickClientObject = this.inputController != null && this.inputController.ObjectPicker != null ? this.inputController.ObjectPicker.HFHBOINDMAJ : null;

                            List<EquipmentObject> ICGEHBHPFOA = (List<EquipmentObject>)Utils.GetInstanceField(this.player, "ICGEHBHPFOA");
                            if (ICGEHBHPFOA != null)
                            {
                                foreach (EquipmentObject obj in ICGEHBHPFOA)
                                {
                                    Utils.Log(obj.EquipmentSlot + "=" + obj.name);
                                }
                            }
                        }
                        this.leftMouseDown = true;
                    }
                    else
                    {
                        this.leftMouseDown = false;
                    }

                    if (Input.GetMouseButtonDown(1))
                    {
                        this.rightMouseDown = true;
                    }
                    else
                    {
                        this.rightMouseDown = false;
                    }
                }

                //Utils.Log("update = " + update.ToString());
                if (update > 0.5f)
                {
                    //Utils.Log("Update!");
                    update = 0;

                    var updateWatch = new System.Diagnostics.Stopwatch();
                    updateWatch.Start();

                    this.applicationController = GameObjectSingleton<ApplicationController>.DJCGIMIDOPB;

                    if (this.applicationController != null)
                    {
                        this.player = this.applicationController.Player;

                        if (VERBOSE_DEBUG && !this.Intercepting)
                        {
                            // The logic and the obfuscated names used in this logic
                            // can be inferred by looking at SendMessage() in MessageCore.dll
                            if (applicationController != null && applicationController.GPLIHPHPNKL != null)
                            {
                                AOHDPDIPMKO GEDMGBHAEAB = (AOHDPDIPMKO)Utils.GetInstanceField<JFFJBADOENN>(applicationController.GPLIHPHPNKL, "GEDMGBHAEAB");
                                if (GEDMGBHAEAB != null)
                                {
                                    try
                                    {
                                        Utils.SetInstanceField(GEDMGBHAEAB, "HCBFBBABLDC", true);
                                        Utils.Log("INTERCEPTING ENABLED!");
                                    }
                                    catch (Exception ex)
                                    {
                                        Utils.Log(ex.ToString());
                                    }
                                    finally
                                    {
                                        this.Intercepting = true;
                                    }
                                }
                            }
                        }
                    }

                    this.inputController = InputController.Instance;

                    Queue<ClientCommand> ClientCommandsQueue = null;
                    ClientCommand[] ClientCommandsArray = null;
                    ClientCommand ClientCommand = null;

                    if (this.ProcessId != -1 && this.ClientCommandsMemoryMap != null)
                    {
                        try
                        {
                            ClientCommandsMemoryMap.ReadMemoryMap(out this.ClientCommandId, out ClientCommandsArray);
                        }
                        catch (Exception ex)
                        {
                            Utils.Log("Error reading memory map: " + ex.ToString());
                        }
                        if (ClientCommandsArray != null)
                        {
                            ClientCommandsQueue = new Queue<ClientCommand>(ClientCommandsArray);
                            if (ClientCommandsQueue.Count > 0)
                            {
                                Utils.Log("Command found");
                                ClientCommand = ClientCommandsQueue.Dequeue();
                                ClientCommandId++;
                                try
                                {
                                    ProcessClientCommand(ClientCommand);
                                    Utils.Log("Command " + ClientCommandId.ToString() + " processed");
                                }
                                catch (Exception ex)
                                {
                                    Utils.Log("Error processing client command: " + ex.ToString());
                                }
                            }
                        }
                    }

                    try
                    {
                        UpdateClientStatus();
                    } catch (Exception ex)
                    {
                        Utils.Log("Error updating status: " + ex.ToString());
                    }

                    if (this.ProcessId != -1 && this.ClientCommandsMemoryMap != null)
                    {
                        if (ClientCommandId > 0 && ClientCommandsQueue != null)
                        {
                            try
                            {
                                ClientCommandsMemoryMap.WriteMemoryMap(ClientCommandId, ClientCommandsQueue.ToArray());
                            }
                            catch (Exception ex)
                            {
                                Utils.Log("Error reading memory map: " + ex.ToString());
                            }
                        }
                    }

                    updateWatch.Stop();
                    //Utils.Log("Update finished in " + updateWatch.ElapsedMilliseconds.ToString());
                }
                //Utils.Log("Update() finish");
            }
            catch (Exception ex)
            {
                Utils.Log(ex.ToString());
                Utils.Log(ex.StackTrace);
            }
        }

        private void OnGUI()
        {
        }

        void OnEnable()
        {
            Utils.Log("OnEnable");

            try
            {
                if (VERBOSE_DEBUG)
                {
                    Utils.Log("VERBOSE_DEBUG enabled!");
                    TraceModule module = new TraceModule("messagelog.");
                    module.AddDebugConsoleListener(SourceLevels.All);
                    ShardEngineDebug.AddTraceModule("messagelog.", module);
                    ShardEngineDebug.PushDebugScope("messagelog.");
                }
            }
            catch (Exception ex)
            {
                Utils.Log(ex.ToString());
            }
        }

        void OnDisable()
        {
            Utils.Log("OnDisable");
        }
    }
}
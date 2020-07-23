function ExtractFirstId(ids)
    local first_id = nil
    for id in string.gmatch(ids, "([^,]+)") do
        first_id = id
        break
    end
    return first_id
end

while true do
    FindItem("flute", BACKPACKID)
    fluteId = ExtractFirstId(FINDITEMID)
    SayCustom(".x use " .. fluteId .. " Play")
    sleep(1500)
end

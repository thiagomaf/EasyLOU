require "math"

function round(num, numDecimalPlaces)
  local mult = 10^(numDecimalPlaces or 0)
  return math.floor(num * mult + 0.5) / mult
end

function distance2d(x1,y1,x2,y2)
	if tonumber(x1) == nil or
		tonumber(y1) == nil or
		tonumber(x2) == nil or
		tonumber(y2) == nil then
		return -1
	end

	return math.sqrt((x2 - x1) ^ 2 + (y2 - y1) ^ 2)
end

function distance(x1,y1,z1,x2,y2,z2)
	if tonumber(x1) == nil or
		tonumber(y1) == nil or
		tonumber(z1) == nil or
		tonumber(x2) == nil or
		tonumber(y2) == nil or
		tonumber(z2) == nil then
		return -1
	end
	
	return math.sqrt((x2 - x1) ^ 2 + (y2 - y1) ^ 2 + (z2 - z1) ^ 2)
end

function ExtractFirstId(ids)
    local first_id = nil
    for id in string.gmatch(ids, "([^,]+)") do
        first_id = id
    	break
    end
    return first_id
end

function Equip(item)
    FindItem (item)
	local tool_id = ExtractFirstId(FINDITEMID)
	local cont_id = ExtractFirstId(FINDITEMCNTID)
	
	if tool_id == nil or
		tool_id == "N/A" then
		print(item .. " not found!")
		return	
	end		
	
	if tostring(cont_id) == tostring(CHARID) then
		print(item .. " already equipped!")
		return	
	end
	
	print("Equipping " .. item)
	SayCustom(".x use " .. tool_id .. " Equip")
end

function GetWeight(id)
    GetTooltip(id)
    local output = nil
    for line in string.gmatch(TOOLTIPTEXT, '([^|]+)') do
        if string.match(line, "Weight:") then
            for weight in string.gmatch(line, "Weight:%s(.*)") do
                --print(weight)
                output = weight
            end
        end
    end
    return output
end

function GetQuantity(id)
    GetTooltip(id)
    local output = nil
    for line in string.gmatch(TOOLTIPTEXT, '([^|]+)') do
        if string.match(line, "Contains:") then
        	for itemCount in string.gmatch(line, "Contains:%s(%d+)") do
            	--print(itemCount)
            	output = itemCount
        	end
    	end
    end
    return output
end

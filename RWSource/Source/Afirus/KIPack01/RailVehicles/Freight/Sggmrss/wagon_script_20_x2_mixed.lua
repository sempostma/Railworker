
DEBUG = true
NUMBER_OF_CARGOS = 59

local oldRvNumber = nil
local debugEnabled = true;
local selectedSkinNumber1 = nil
local selectedSkinNumber2 = nil
local cargoOverlay = nil
local wagonOverlay = nil

local function debugPrint(str)
  if (debugEnabled==true) then
    Print(str)
  end
end

local function hash(input)
    local sum = 0
    for i = 1, string.len(input) do
        sum = sum + string.byte(string.sub(input, i, i))
    end
    local mod = sum - (math.floor(sum / NUMBER_OF_CARGOS) * NUMBER_OF_CARGOS)
    return mod + 1
end

local function chooseContainer(rv)

    debugPrint("RV number: " .. rv)

    rv = string.lower(rv)

    -- Check for the rvNumber postfix and extract the number if present
    
    -- Find the position of the underscore character
    local underscorePos1 = nil
    for i = 1, string.len(rv) do
        if string.sub(rv, i, i) == "_" then
            underscorePos1 = i
            break
        end
    end

    if (underscorePos1 == nil) then
        local h1 = hash(rv .. "cargo_1")
        local h2 = hash(rv .. "cargo_2")
        if (h2 == h1) then
            h2 = h2 + 1
            if (h2 > NUMBER_OF_CARGOS) then h2 = 1 end
        end
    
        return h1, h2
    end

    local rvPostfix1 = nil
    local underscorePos2 = nil
    if (underscorePos1) then
        for i = underscorePos1 + 1, string.len(rv) do
            if string.sub(rv, i, i) == "_" then
                underscorePos2 = i
                rvPostfix1 = string.sub(rv, underscorePos1 + 1, i - 1)
                debugPrint("Found rv postfix 1: " .. rvPostfix1)
                break
            end
        end
    end

    local rvPostfix2 = nil
    if (underscorePos2) then
        rvPostfix2 = string.sub(rv, underscorePos2 + 1)
        debugPrint("Found rv postfix 2: " .. rvPostfix2)
    elseif (underscorePos1) then
        rvPostfix1 = string.sub(rv, underscorePos1 + 1)
        debugPrint("Found rv postfix 1 at the end: " .. rvPostfix1)
    end

    local rvCargoNumber1 = tonumber(rvPostfix1)
    local rvCargoNumber2 = tonumber(rvPostfix2)
    if (rvCargoNumber1 == rvCargoNumber2 and rvCargoNumber1 ~= nil) then
        rvCargoNumber2 = rvCargoNumber2 + 1
        if (rvCargoNumber2 > NUMBER_OF_CARGOS) then rvCargoNumber2 = 1 end
    end

    return rvCargoNumber1, rvCargoNumber2
end

local function showSelectedContainer()
    -- Deactivate all
    for i = 1, NUMBER_OF_CARGOS do
        Call("ActivateNode", "cargo_" .. i, 0)
    end
    
    if selectedSkinNumber1 then
        Call("ActivateNode", "cargo_" .. selectedSkinNumber1, 1)
        Call("cargo_" .. selectedSkinNumber1 .. ":setNearPosition", 0, 3.1, 0)
        debugPrint("Activated selected skin node for cargo 1: " .. selectedSkinNumber1)
    end
    if selectedSkinNumber2 then
        Call("ActivateNode", "cargo_" .. selectedSkinNumber2, 1)
        SysCall("*:setNearPosition", 0, -3.1, 0)
        debugPrint("Activated selected skin node for cargo 2: " .. selectedSkinNumber2)
    end
end

-- Function that is called once upon scenario initialisation but before route load. 
--- Should be used to set up variables/simulation elements of a script at the start of a scenario e.g. turning off/on lights.
function Initialise()
    FirstRun = true

    Call("BeginUpdate")
end

local backoffTime = 0

function Update(frameTime)
    if (FirstRun) then
        backoffTime = backoffTime - frameTime
        if (backoffTime > 0) then
            return
        end

        local isEditor = Call("GetScenarioTime") == nil

        if (isEditor) then
            -- If we are in the editor we want to reload the containers based on the RV number
            -- The user can change the RV number in the editor so we reload the random skin
            backoffTime = 2 -- 2 seconds
        else
            FirstRun = false

            -- Set EndUpdate for performance sake (keep FirstRun = false as backup)
            Call("EndUpdate")
        end

        -- Sluitsein

        local wagonFront = Call("SendConsistMessage", 0, 0, 0)
        local wagonBack = Call("SendConsistMessage", 0, 0, 1)

        debugPrint("WagonFront: " .. wagonFront)
        debugPrint("wagonBack: " .. wagonBack)

        local showSluitsein = 0
        if (wagonFront + wagonBack == 1) then showSluitsein = 1 end
        
        debugPrint("Activate sluitsein: " .. showSluitsein)

        Call("ActivateNode", "sluitsein", showSluitsein)

        -- Rail vehicle number
        
        local rv = Call("GetRVNumber")

        if rv == oldRvNumber then return end
        
        -- Prevent duplicate calls to chooseContainer()
        oldRvNumber = rv
        local newSkinNumber1, newSkinNumber2 = chooseContainer(rv)

        -- Prevent duplicate calls to showSelectedContainer()
        if (newSkinNumber1 == selectedSkinNumber1 and newSkinNumber2 == selectedSkinNumber2) then return end
        
        selectedSkinNumber1 = newSkinNumber1
        selectedSkinNumber2 = newSkinNumber2
        showSelectedContainer()
    end
end

function OnConsistMessage(message, argument, direction)
    if message == 0 then return end

    Call("SendConsistMessage", message, argument, direction)

    debugPrint("SendConsistMessage")
end

function OnControlValueChange ( name, index, value )
    debugPrint("OnControlValueChange: " .. name .. ", " .. index .. ", " .. value)
end

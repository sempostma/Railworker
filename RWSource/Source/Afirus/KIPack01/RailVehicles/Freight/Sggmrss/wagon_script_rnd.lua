
DEBUG = true
NUMBER_OF_CARGOS = 36

local oldRvNumber = nil
local debugEnabled = true;
local selectedSkinNumber1 = nil
local selectedSkinNumber2 = nil
local oldShowSluitsein = 1
local showSluitsein = 0

local function debugPrint(str)
  if (debugEnabled==true) then
    Print(str)
  end
end

local function mod(input, power) 
    if (input == nil or power == nil) then return nil end
    return input - (math.floor(input / power) * power)
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
        -- do a descent attempt at showing some cargo when the rv number is invalid (for locoswapped with invalid rv numbers for example)
        local h1 = hash(rv .. "cargo_1")
        local h2 = hash(rv .. "cargo_2")
        if (mod(h1, 2) == mod(h2, 2)) then
            h2 = nil
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
        rvCargoNumber2 = rvCargoNumber2 - 1
        if (rvCargoNumber2 < 1) then rvCargoNumber2 = 2 end
    end

    if (mod(rvCargoNumber1, 2) == mod(rvCargoNumber2, 2)) then
        rvCargoNumber2 = nil
    end

    return rvCargoNumber1, rvCargoNumber2
end

local function showOrHideNodes()
    -- Deactivate all
    Call("ActivateNode", "sluitsein", showSluitsein)

    if selectedSkinNumber1 then
        debugPrint("Enable skin number 1: " .. selectedSkinNumber1)

        Call("Cargo_A:ActivateNode", "all", 0)
        Call("Cargo_A:ActivateNode", "cargo_" .. selectedSkinNumber1, 1)
        debugPrint("Activated selected skin node for Cargo A: " .. selectedSkinNumber1)
    end
    if selectedSkinNumber2 then
        debugPrint("Enable skin number 2: " .. selectedSkinNumber2)

        Call("Cargo_B:ActivateNode", "all", 0)
        Call("Cargo_B:ActivateNode", "cargo_" .. selectedSkinNumber2, 1)
        debugPrint("Activated selected skin node for Cargo B: " .. selectedSkinNumber2)
    end
end

-- Function that is called once upon scenario initialisation but before route load. 
--- Should be used to set up variables/simulation elements of a script at the start of a scenario e.g. turning off/on lights.
function Initialise()
    FirstRun = true
    Call("Cargo_A:ActivateNode", "all", 0)
    Call("Cargo_B:ActivateNode", "all", 0)
    Call("BeginUpdate")
end

-- Called after a save is loaded
function OnResume()
    FirstRun = true
    Call("Cargo_A:ActivateNode", "all", 0)
    Call("Cargo_B:ActivateNode", "all", 0)
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

        -- debugPrint("WagonFront: " .. wagonFront)
        -- debugPrint("wagonBack: " .. wagonBack)

        if (wagonFront + wagonBack == 1) then showSluitsein = 1 end
        
        -- Rail vehicle number
        
        local rv = Call("GetRVNumber")

        if (rv == oldRvNumber and showSluitsein == oldShowSluitsein) then return end

        local newSkinNumber1 = selectedSkinNumber1
        local newSkinNumber2 = selectedSkinNumber2

        debugPrint("Rv 2 number: " .. (rv or ""))

        if (rv ~= oldRvNumber) then
            oldRvNumber = rv
            newSkinNumber1, newSkinNumber2 = chooseContainer(rv)
        end

        if (newSkinNumber1 ~= selectedSkinNumber1 or newSkinNumber2 ~= selectedSkinNumber2) then 
            debugPrint("Setting container and sluitsein nodes")
            -- sluitsein and cargo
            selectedSkinNumber1 = newSkinNumber1
            selectedSkinNumber2 = newSkinNumber2
            oldShowSluitsein = showSluitsein
            showOrHideNodes()
        elseif (showSluitsein ~= oldShowSluitsein) then
            debugPrint("Activate sluitsein: " .. showSluitsein)
            oldShowSluitsein = showSluitsein
            Call("ActivateNode", "sluitsein", showSluitsein)
        end
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


DEBUG = true
NUMBER_OF_CARGOS = 36

local oldRvNumber = nil
local debugEnabled = true;
local selectedSkinNumber = nil
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
    local underscorePos = nil
    for i = 1, string.len(rv) do
        if string.sub(rv, i, i) == "_" then
            underscorePos = i
            break
        end
    end

    if underscorePos then
        -- Extract the part after the underscore
        local rvPostfix = string.sub(rv, underscorePos + 1)
        local rvCargoNumber = tonumber(rvPostfix)

        if (rvCargoNumber) then
            debugPrint("An RV offset will be used: " .. rvCargoNumber)
            return rvCargoNumber
        end
    end

    debugPrint("No underscore pos found or an invalid underscore position found. Falling back to hash function to determine rv vargo number. Found rv number: " .. rv)

    return hash(rv)
end

local function showSelectedContainer()
    -- Deactivate all
    for i = 1, NUMBER_OF_CARGOS do
        Call("ActivateNode", "cargo_" .. i, 0)
    end
    
    -- safety check
    if (selectedSkinNumber == nil) then selectedSkinNumber = 1 end

    Call("ActivateNode", "cargo_" .. selectedSkinNumber, 1)
    debugPrint("Activated selected skin node: " .. selectedSkinNumber)
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
        local newSkinNumber = chooseContainer(rv)

        -- Prevent duplicate calls to showSelectedContainer()
        if (newSkinNumber == selectedSkinNumber) then return end
        
        selectedSkinNumber = newSkinNumber
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

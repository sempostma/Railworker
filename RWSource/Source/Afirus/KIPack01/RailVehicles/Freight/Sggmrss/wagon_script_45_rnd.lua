
DEBUG = true
NUMBER_OF_CARGOS = 36

local debugEnabled = true;
local selectedSkinNumber = nil
local selectedOverlay = nil

local function debugPrint(str)
  if (debugEnabled==true) then
    Print(str)
  end
end

local function chooseContainer()
    local rv = Call("GetRVNumber")

    debugPrint("RV number: " .. rv)

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
        local rvNumber = tonumber(rvPostfix)

        -- Separate the last character from the numeric part
        local lastChar = nil
        lastChar = string.sub(rvPostfix, -1)
        local validChars = {a = true, b = true, c = true, d = true, e = true}

        if validChars[lastChar] then
            rvNumber = tonumber(string.sub(rvPostfix, 1, -2)) -- Remove the last character
            debugPrint("Postfix letter found: " .. lastChar)
        else
            debugPrint("Postfix letter not found in postfix: " .. rvPostfix)
            rvNumber = tonumber(rvPostfix)
            lastChar = nil
        end

        if rvNumber == nil then
            debugPrint("An RV postfix was provided but it could not be converted to an integer: " .. rvPostfix)
            return 1, lastChar
        elseif rvNumber < 1 then
            debugPrint("An RV postfix was provided but it is less than 1: " .. rvPostfix)
            return 1, lastChar
        elseif rvNumber > NUMBER_OF_CARGOS then
            debugPrint("An RV postfix was provided but its more than the total amount of reskins: " .. rvPostfix)
            return 1, lastChar
        else
            debugPrint("An RV offset will be used: " .. rvNumber)
            return rvNumber, lastChar -- Use minus because the rv_offset starts at 1
        end
    else
        debugPrint("No underscore pos found: " .. rv)
    end

    -- Return 1 if no valid postfix is found
    return 1, nil
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

    Call("ActivateNode", "dirty_a", 0)
    Call("ActivateNode", "dirty_b", 0)
    Call("ActivateNode", "dirty_c", 0)
    Call("ActivateNode", "dirty_d", 0)
    Call("ActivateNode", "dirty_e", 0)

    if (selectedOverlay ~= nil) then
        Call("ActivateNode", "dirty_" .. selectedOverlay, 1)
        debugPrint("Activated selected overlay: " .. selectedOverlay)
    else
        debugPrint("No overlay provided")
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
        
        local newSkinNumber, newOverlay = chooseContainer()

        -- Prevent duplicate calls to showSelectedContainer()
        if (newSkinNumber == selectedSkinNumber and newOverlay == selectedOverlay) then return end
        
        selectedSkinNumber = newSkinNumber
        selectedOverlay = newOverlay
        showSelectedContainer()
    end
end

function OnConsistMessage(message, argument, direction)
    Call("SendConsistMessage", message, argument, direction)

    debugPrint("SendConsistMessage")
end

function OnControlValueChange ( name, index, value )
    debugPrint("OnControlValueChange: " .. name .. ", " .. index .. ", " .. value)
end

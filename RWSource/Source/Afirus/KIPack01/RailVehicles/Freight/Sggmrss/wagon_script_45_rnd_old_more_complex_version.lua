
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

        -- Extract and check the last character (wagon dirtyness)
        local wagonDirtyness = nil
        wagonDirtyness = string.sub(rvPostfix, -1)
        local validWagonDirtynessChars = {a = true, b = true, c = true, d = true, e = true, x = true}

        if validWagonDirtynessChars[wagonDirtyness] then
            rvPostfix = string.sub(rvPostfix, 1, -2) -- Remove the last character (wagon dirtyness)
            rvCargoNumber = tonumber(rvPostfix) -- Remaining numeric part (and possibly cargo dirtyness)
            debugPrint("Wagon dirtyness letter found: " .. wagonDirtyness)
        else
            debugPrint("Wagon dirtyness letter not found in postfix: " .. rvPostfix)
            wagonDirtyness = nil
        end

        -- Extract and check the second last character (cargo dirtyness)
        local cargoDirtyness = nil
        cargoDirtyness = string.sub(rvPostfix, -1)
        local validCargoDirtyNessChars = {a = true, b = true, c = true, d = true, e = true, x = true}

        if validCargoDirtyNessChars[cargoDirtyness] then
            rvPostfix = string.sub(rvPostfix, 1, -2) -- Remove the last character (cargo dirtyness)
            rvCargoNumber = tonumber(rvPostfix) -- Remaining numeric part
            debugPrint("Cargo dirtyness letter found: " .. cargoDirtyness)
        else
            debugPrint("Cargo dirtyness letter not found in postfix: " .. rvPostfix)
            cargoDirtyness = nil
        end

        if wagonDirtyness ~= nil and cargoDirtyness == nil then
            -- It didnt provide a value for the wagon dirtyness but instead for the cargo dirtyness so switch values around
            cargoDirtyness = wagonDirtyness
            wagonDirtyness = nil
        end

        if wagonDirtyness == 'x' then wagonDirtyness = nil end
        if cargoDirtyness == 'x' then cargoDirtyness = nil end

        -- Process rvNumber to determine if it's valid
        if rvCargoNumber == nil then
            debugPrint("An RV postfix was provided but it could not be converted to an integer: " .. rvPostfix)
            return 1, cargoDirtyness, wagonDirtyness
        elseif rvCargoNumber < 0 then
            debugPrint("An RV postfix was provided but it is less than 0: " .. rvPostfix)
            return 1, cargoDirtyness, wagonDirtyness
        elseif rvCargoNumber > 36 then
            debugPrint("An RV postfix was provided but its more than the maximum number of containers: " .. rvPostfix)
            return 1, cargoDirtyness, wagonDirtyness
        else
            debugPrint("An RV offset will be used: " .. rvCargoNumber)
            return rvCargoNumber, cargoDirtyness, wagonDirtyness
        end
    else
        debugPrint("No underscore pos found: " .. rv)
    end

    -- Return 1 if no valid postfix is found
    return 1, nil, nil
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

    Call("ActivateNode", "cargo_dirty_a", 0)
    Call("ActivateNode", "cargo_dirty_b", 0)
    Call("ActivateNode", "cargo_dirty_c", 0)
    Call("ActivateNode", "cargo_dirty_d", 0)
    Call("ActivateNode", "cargo_dirty_e", 0)

    if (cargoOverlay ~= nil) then
        Call("ActivateNode", "cargo_dirty_" .. cargoOverlay, 1)
        debugPrint("Activated selected overlay: " .. cargoOverlay)
    else
        debugPrint("No overlay provided")
    end


    Call("ActivateNode", "wagon_dirty_a", 0)
    Call("ActivateNode", "wagon_dirty_b", 0)
    Call("ActivateNode", "wagon_dirty_c", 0)
    Call("ActivateNode", "wagon_dirty_d", 0)
    Call("ActivateNode", "wagon_dirty_e", 0)

    if (wagonOverlay ~= nil) then
        Call("ActivateNode", "wagon_dirty_" .. wagonOverlay, 1)
        debugPrint("Activated selected overlay: " .. wagonOverlay)
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
        local newSkinNumber, newCargoOverlay, newWagonOverlay = chooseContainer(rv)

        -- Prevent duplicate calls to showSelectedContainer()
        if (newSkinNumber == selectedSkinNumber and newCargoOverlay == cargoOverlay and wagonOverlay == newWagonOverlay) then return end
        
        selectedSkinNumber = newSkinNumber
        cargoOverlay = newCargoOverlay
        wagonOverlay = newWagonOverlay
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

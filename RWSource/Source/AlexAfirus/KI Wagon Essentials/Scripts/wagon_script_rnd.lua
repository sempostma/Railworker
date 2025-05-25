
DEBUG = false
NUMBER_OF_CARGOS = 36

local debugEnabled = false;
local selectedSkinNumber1 = nil
local selectedSkinNumber2 = nil
local selectedSkinNumber3 = nil
local selectedSkinNumber4 = nil
local showSluitsein = 0
local random = 0; -- math.random(): Number between 0 and 1
local random2 = 0; -- math.random(10): Number between 0 and 10
local selectedSpecialNode1 = nil
local selectedSpecialNode2 = nil

local backoffTime = 2
local skipFrames = 10
local skipSeconds = 2
local veryFirstRun = true
local firstRun = false
local rv = nil

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
    
    -- Reset special nodes
    local specialNode1 = nil
    local specialNode2 = nil
    
    -- Find both ! and : characters to determine which comes first
    local exclamPos1 = nil
    local colonPos1 = nil
    
    for i = 1, string.len(rv) do
        local char = string.sub(rv, i, i)
        if char == "!" and exclamPos1 == nil then
            exclamPos1 = i
        elseif char == ":" and colonPos1 == nil then
            colonPos1 = i
        end
        
        -- If we found both, we can stop searching
        if exclamPos1 ~= nil and colonPos1 ~= nil then
            break
        end
    end
    
    -- If : comes before ! or there's no !, skip special node extraction
    if (colonPos1 ~= nil and (exclamPos1 == nil or colonPos1 < exclamPos1)) then
        debugPrint("Colon found before exclamation mark, skipping special node extraction")
    else
        -- Process special nodes if ! was found
        if exclamPos1 ~= nil then
            -- Find the end of the first node name (either next ! or :)
            local nodeEndPos1 = nil
            for i = exclamPos1 + 1, string.len(rv) do
                local char = string.sub(rv, i, i)
                if char == "!" or char == ":" then
                    nodeEndPos1 = i
                    break
                end
            end
            
            if nodeEndPos1 == nil then
                nodeEndPos1 = string.len(rv) + 1
            end
            
            -- Extract the first node name
            specialNode1 = string.sub(rv, exclamPos1 + 1, nodeEndPos1 - 1)
            if specialNode1 and string.len(specialNode1) > 0 then
                debugPrint("Found special node 1: " .. specialNode1)
            else
                specialNode1 = nil
            end
            
            -- Find second ! character
            local exclamPos2 = nil
            for i = nodeEndPos1, string.len(rv) do
                if string.sub(rv, i, i) == "!" then
                    exclamPos2 = i
                    break
                end
            end
            
            if exclamPos2 ~= nil then
                -- Find the end of the second node name (either next ! or :)
                local nodeEndPos2 = nil
                for i = exclamPos2 + 1, string.len(rv) do
                    local char = string.sub(rv, i, i)
                    if char == "!" or char == ":" then
                        nodeEndPos2 = i
                        break
                    end
                end
                
                if nodeEndPos2 == nil then
                    nodeEndPos2 = string.len(rv) + 1
                end
                
                -- Extract the second node name
                specialNode2 = string.sub(rv, exclamPos2 + 1, nodeEndPos2 - 1)
                if specialNode2 and string.len(specialNode2) > 0 then
                    debugPrint("Found special node 2: " .. specialNode2)
                else
                    specialNode2 = nil
                end
            end
        end
    end
    
    -- Now process the RV number for cargo containers
    -- We already found the first colon position above, but if we didn't, search for it now
    if colonPos1 == nil then
        for i = 1, string.len(rv) do
            if string.sub(rv, i, i) == ":" then
                colonPos1 = i
                break
            end
        end
    end

    if (colonPos1 == nil) then
        -- do a descent attempt at showing some cargo when the rv number is invalid (for locoswapped with invalid rv numbers for example)
        local h1 = hash(rv .. "cargo_1")
        local h2 = hash(rv .. "cargo_2")
        local h3 = hash(rv .. "cargo_3")
        local h4 = hash(rv .. "cargo_4")

        return h1, h2, h3, h4
    end

    local rvPostfix1 = nil
    local colonPos2 = nil
    if (colonPos1) then
        for i = colonPos1 + 1, string.len(rv) do
            if string.sub(rv, i, i) == ":" then
                colonPos2 = i
                rvPostfix1 = string.sub(rv, colonPos1 + 1, i - 1)
                debugPrint("Found rv postfix 1: " .. rvPostfix1)
                break
            end
        end
    end

    local rvPostfix2 = nil
    local colonPos3 = nil
    if (colonPos2) then
        for i = colonPos2 + 1, string.len(rv) do
            if string.sub(rv, i, i) == ":" then
                colonPos3 = i
                rvPostfix2 = string.sub(rv, colonPos2 + 1, i - 1)
                debugPrint("Found rv postfix 2: " .. rvPostfix2)
                break
            end
        end
    elseif (colonPos1) then
        rvPostfix1 = string.sub(rv, colonPos1 + 1)
        debugPrint("Found rv postfix 1 at the end: " .. rvPostfix1)
    end
    
    local rvPostfix3 = nil
    local colonPos4 = nil
    if (colonPos3) then
        for i = colonPos3 + 1, string.len(rv) do
            if string.sub(rv, i, i) == ":" then
                colonPos4 = i
                rvPostfix3 = string.sub(rv, colonPos3 + 1, i - 1)
                debugPrint("Found rv postfix 3: " .. rvPostfix3)
                break
            end
        end
    elseif (colonPos2) then
        rvPostfix2 = string.sub(rv, colonPos2 + 1)
        debugPrint("Found rv postfix 2 at the end: " .. rvPostfix2)
    end
    
    local rvPostfix4 = nil
    if (colonPos4) then
        rvPostfix4 = string.sub(rv, colonPos4 + 1)
        debugPrint("Found rv postfix 4: " .. rvPostfix4)
    elseif (colonPos3) then
        rvPostfix3 = string.sub(rv, colonPos3 + 1)
        debugPrint("Found rv postfix 3 at the end: " .. rvPostfix3)
    end

    local rvCargoNumber1 = tonumber(rvPostfix1)
    local rvCargoNumber2 = tonumber(rvPostfix2)
    local rvCargoNumber3 = tonumber(rvPostfix3)
    local rvCargoNumber4 = tonumber(rvPostfix4)
    
    return specialNode1, specialNode2, rvCargoNumber1, rvCargoNumber2, rvCargoNumber3, rvCargoNumber4
end

local function showOrHideNodes()
    -- Deactivate all
    Call("ActivateNode", "sluitsein", showSluitsein)
    
    -- Activate special nodes from RV number
    if selectedSpecialNode1 ~= nil then
        debugPrint("Deactivating known special nodes")
        Call("ActivateNode", "c20", 0)
        Call("ActivateNode", "c21", 0)
        Call("ActivateNode", "c30", 0)
        Call("ActivateNode", "c40", 0)

        debugPrint("Activating special node 1: " .. selectedSpecialNode1)
        Call("ActivateNode", selectedSpecialNode1, 1)
    end
    
    if selectedSpecialNode2 ~= nil then
        debugPrint("Activating special node 2: " .. selectedSpecialNode2)
        Call("ActivateNode", selectedSpecialNode2, 1)
    end

    Call("Cargo_A:ActivateNode", "all", 0)
    if selectedSkinNumber1 then
        debugPrint("Enable skin number 1: " .. selectedSkinNumber1)

        Call("Cargo_A:ActivateNode", "cargo_" .. selectedSkinNumber1, 1)
        Call("Cargo_A:ActivateNode", "main", 1)
        debugPrint("Activated selected skin node for Cargo A: " .. selectedSkinNumber1)
    end
    
    Call("Cargo_B:ActivateNode", "all", 0)
    if selectedSkinNumber2 then
        debugPrint("Enable skin number 2: " .. selectedSkinNumber2)

        Call("Cargo_B:ActivateNode", "cargo_" .. selectedSkinNumber2, 1)
        Call("Cargo_B:ActivateNode", "main", 1)
        debugPrint("Activated selected skin node for Cargo B: " .. selectedSkinNumber2)
    end
    
    Call("Cargo_C:ActivateNode", "all", 0)
    if selectedSkinNumber3 then
        debugPrint("Enable skin number 3: " .. selectedSkinNumber3)

        Call("Cargo_C:ActivateNode", "cargo_" .. selectedSkinNumber3, 1)
        Call("Cargo_C:ActivateNode", "main", 1)
        debugPrint("Activated selected skin node for Cargo C: " .. selectedSkinNumber3)
    end
    
    Call("Cargo_D:ActivateNode", "all", 0)
    if selectedSkinNumber4 then
        debugPrint("Enable skin number 4: " .. selectedSkinNumber4)

        Call("Cargo_D:ActivateNode", "cargo_" .. selectedSkinNumber4, 1)
        Call("Cargo_D:ActivateNode", "main", 1)
        debugPrint("Activated selected skin node for Cargo D: " .. selectedSkinNumber4)
    end
    
    Call("ActivateNode", "sluitsein", showSluitsein)
end

local function setCargoNodes() 
    -- Rail vehicle number
    selectedSpecialNode1, selectedSpecialNode2, selectedSkinNumber1, selectedSkinNumber2, selectedSkinNumber3, selectedSkinNumber4 = chooseContainer(rv)

    debugPrint("Setting container and sluitsein nodes")
    -- sluitsein and cargo
    showOrHideNodes()
end

local function setSluitsein()
    -- Sluitsein

    local wagonFront = Call("SendConsistMessage", 0, 0, 0)
    local wagonBack = Call("SendConsistMessage", 0, 0, 1)

    -- debugPrint("WagonFront: " .. wagonFront)
    -- debugPrint("wagonBack: " .. wagonBack)

    if (wagonFront + wagonBack == 1) then showSluitsein = 1 end

    debugPrint("Activate sluitsein: " .. showSluitsein)
    Call("ActivateNode", "sluitsein", showSluitsein)
end

-- Function that is called once upon scenario initialisation but before route load. 
--- Should be used to set up variables/simulation elements of a script at the start of a scenario e.g. turning off/on lights.
function Initialise()
    firstRun = true
    random = math.random()
    random2 = math.random(10)
    -- Attempt to remove the nodes even if they may not already have been loaded. Because we want to get rid of the extra geometry as fast as possible.
    rv = Call("GetRVNumber")
    setCargoNodes()
    Call("BeginUpdate")
end

-- Called after a save is loaded
function OnResume()
    firstRun = true
    random = math.random()
    random2 = math.random(10)
    -- Attempt to remove the nodes even if they may not already have been loaded. Because we want to get rid of the extra geometry as fast as possible.
    rv = Call("GetRVNumber")
    setCargoNodes()
    Call("BeginUpdate")
end

function Update(frameTime)
    if (firstRun) then
        if (veryFirstRun) then
            veryFirstRun = false
            setSluitsein()
            setCargoNodes()
        else
            -- Second run as a backup
            if (backoffTime > 0) then
                backoffTime = backoffTime - frameTime
                return
            end
            if (skipFrames > 0) then
                skipFrames = skipFrames - 1
                return
            end
            if (skipSeconds > 0) then
                skipSeconds = skipSeconds - frameTime
                return
            end
        end

        local isEditorOrFreeRoam = Call("GetScenarioTime") == nil

        if (isEditorOrFreeRoam) then
            -- If we are in the editor we want to reload the containers based on the RV number
            -- The user can change the RV number in the editor so we reload the random skin
            rv = Call("GetRVNumber")
            setSluitsein();

            -- This prevents lag spikes because this is called on all contains at the same time (tested, i dont know why)
            -- The random numbers ensure that the load is distributed
            backoffTime = 2 + random; -- 2 seconds
            skipFrames = random2;
        else
            firstRun = false
            debugPrint("End Update")
            -- Set EndUpdate for performance sake (keep firstRun = false as backup)
            Call("EndUpdate")
        end

        setCargoNodes()
    end
end

function OnConsistMessage(message, argument, direction)
    if message == 0 then return end

    Call("SendConsistMessage", message, argument, direction)

    debugPrint("SendConsistMessage")
end

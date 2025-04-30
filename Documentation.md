# RakNet

The **RakNet** library provides an interface for the RakNet networking framework within Roblox.

> **Note**
> This documentation covers only stable and verified functions. Experimental or unstable features are excluded. For additional context, refer to the [original source](https://gist.github.com/jLn0n/12411f276d864ee4747d318f4d5ac2a9).

---

## rnet.blockcreates

```lua
function rnet.blockcreates(arg1: boolean)
```

Enables or disables filtering of `"ID_NEW_INSTANCE"` packets, currently limited to blocking `RightGrip` creation.

### Parameters
* `arg1`: A boolean indicating whether to filter create instance packets (`true` to enable, `false` to disable).

### Example
```lua
rnet.blockcreates(true) -- Blocks RightGrip creation packets.
```

---

## rnet.blockdeletes

```lua
function rnet.blockdeletes(arg1: boolean)
```

Enables or disables filtering of `"ID_DELETE_INSTANCE"` packets, preventing client-side deletions from being sent to the server.

### Parameters
* `arg1`: A boolean indicating whether to filter delete instance packets (`true` to enable, `false` to disable).

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local character = player.Character

rnet.blockdeletes(true)
character["Left Leg"]:Destroy() -- Deletion is not sent to the server.
```

---

## rnet.destroy

```lua
function rnet.destroy(instance: Instance)
```

Sends a `"DELETE_INSTANCE"` packet to destroy the specified `instance`.

### Parameters
* `instance`: The instance to destroy.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local character = player.Character

rnet.destroy(character.Torso.Neck) -- Destroys the neck weld
```

---

## rnet.disconnect

```lua
function rnet.disconnect()
```

Destroys the client's replicator, disconnecting the client from the server.

### Parameters
* None.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local character = player.Character

character.Humanoid.Died:Connect(function()
    rnet.disconnect() -- Disconnects the client upon death.
end)
```

---

## rnet.equiptool

```lua
function rnet.equiptool(tool: Tool, weld: Weld?, alsoUnequip: boolean?)
```

Equips the specified `tool` to the character.

### Parameters
* `tool`: The tool to equip.
* `weld`: An optional custom weld for the tool.
* `alsoUnequip`: An optional boolean indicating whether to unequip the tool afterward.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local backpack = player.Backpack

rnet.equiptool(backpack:GetChildren()[1]) -- Equips the first tool in the backpack.
```

---

## rnet.fireevent

```lua
function rnet.fireevent(instance: Instance, eventName: string, ...: any)
```

Triggers the specified event (`eventName`) on the given `instance` with the provided arguments. This function is equivelent to Synapse/SW's `firesignal`.

### Parameters
* `instance`: The target instance on which to fire the event.
* `eventName`: The name of the event to trigger.
* `...`: Variable arguments to pass to the event.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local backpack = player.Backpack
local character = player.Character

-- Equips the first tool in the player's backpack on the server.
rnet.fireevent(character.Humanoid, "ServerEquipTool", backpack:GetChildren()[1])
```

---

## rnet.fireremote

```lua
function rnet.fireremote(remote: RemoteEvent | RemoteFunction, count: number, ...)
```

Fires the specified `remote` (RemoteEvent or RemoteFunction) `count` times with the given arguments.

### Parameters
* `remote`: The remote to fire.
* `count`: The number of times to fire the remote.
* `...`: Arguments to pass to the remote.

### Example
```lua
local remote = game:GetService("ReplicatedStorage").DefaultMakeChatSystemEvents.SayMessageRequest

rnet.fireremote(remote, 5, "message", "All") -- Sends the message "message" to all players 5 times.
```

---

## rnet.getevents

```lua
function rnet.getevents(instance: Instance): array
```

Retrieves a table containing the names and metadata of events associated with the specified `instance`.

### Parameters
* `instance`: The instance to query for events.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local character = player.Character
local eventNames = rnet.getevents(character.Humanoid)

table.foreach(eventNames, print) -- Prints the list of event names.
```

---

## rnet.getproperties

```lua
function rnet.getproperties(instance: Instance): array
```

Returns a table of hidden properties for the specified `instance`.

### Parameters
* `instance`: The instance to query for hidden properties.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local character = player.Character
local hiddenProperties = rnet.getproperties(character.Humanoid)

table.foreach(hiddenProperties, print) -- Prints the list of hidden properties.
```

---

## rnet.nextpacket

```lua
function rnet.nextpacket(): string
```

Retrieves the most recent packet sent by the client to the server.

### Parameters
* None.

### Example
```lua
-- See the `rnet.readeventpacket` example for usage.
```

---

## rnet.readeventpacket

```lua
function rnet.readeventpacket(packet: string): dictionary
```

Decodes a packet and returns a dictionary containing event information. This function is useful for analyzing packet-based events.

### Return Value
A dictionary with the following fields:
| Field       | Type     | Description                              |
|-------------|----------|------------------------------------------|
| `source`    | Instance | The instance associated with the event.  |
| `name`      | string   | The name of the triggered event.         |
| `arguments` | array    | The arguments passed to the event.       |

### Parameters
* `packet`: The packet to decode.

### Example
```lua
local packetNumber = 0

while true do
    task.wait()
    local packetData = rnet.nextpacket()
    
    if packetData.id == 0x83 and packetData.subid == 0x7 then
        pcall(function()
            local packetInfo = rnet.readeventpacket(packetData)
            print(string.format("Packet #%d: Event [%s] on %s", 
                packetNumber, 
                packetInfo.name, 
                packetInfo.source:GetFullName()
            ))
        end)
    end
    packetNumber += 1
end
```

**Note**: This function may exhibit inconsistent behavior in certain scenarios.

---

## rnet.send

```lua
function rnet.send(packetsList: array)
```

Sends custom packets specified in `packetsList`. Packet values must be in byte-hex format.

> **Warning**
> Sending arbitrary packets may result in a ban. Use with caution.

### Parameters
* `packetsList`: An array of packets to send.

### Example
```lua
rnet.send({{0x2, 0x65, 0x108, 0x69}}) -- Sends a custom packet (example).
```

---

## rnet.sendphysics

```lua
function rnet.sendphysics(position: CFrame)
```

Sends physics packets to update the character's position (as a `CFrame`) to other clients.

### Parameters
* `position`: The `CFrame` representing the character's new position.

### Example
```lua
local position = Vector3.new(0, -69420, 0)

while true do
    task.wait()
    -- Teleports the character to (0, -69420, 0), making you invisible to other players
    rnet.sendphysics(CFrame.identity + position)
end
```

---

## rnet.setfilter

```lua
function rnet.setfilter(filteringPacketIds: array)
```

Filters packets by their IDs or sub-IDs, preventing the client from sending packets that match the specified criteria.

### Parameters
* `filteringPacketIds`: An array of packet IDs or sub-IDs to filter.

### Example
```lua
-- Filters packets starting with the specified ID sequence.
rnet.setfilter({{0x2, 0x65, 0x108, 0x69}})
```

---

## rnet.setparent

```lua
function rnet.setparent(instance: Instance, newParent: Instance)
```

Sets the parent of the specified `instance` to `newParent`.

### Parameters
* `instance`: The instance to reparent.
* `newParent`: The new parent instance.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local character = player.Character
local humanoid = character.Humanoid

rnet.setparent(humanoid, workspace) -- Sets the humanoid's parent to the workspace.
```

---

## rnet.setphysicsrootpart

```lua
function rnet.setphysicsrootpart(instance: BasePart)
```

Attempts to set the specified `instance` as the root part for character physics calculations.

**Issue**: This function is currently non-functional.

### Parameters
* `instance`: The instance to set as the physics root part.

### Example
```lua
-- No example available until the function is operational.
```

---

## rnet.setproperty

```lua
function rnet.setproperty(instance: Instance, propertyName: string, value: any)
```

Sets the value of a hidden property (`propertyName`) on the specified `instance`.

### Parameters
* `instance`: The instance to modify.
* `propertyName`: The name of the hidden property to set.
* `value`: The value to assign to the property.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local character = player.Character
local humanoid = character.Humanoid

rnet.setproperty(humanoid, "AHiddenProperty", true) -- Sets a hidden property (example).
```

---

## rnet.sit

```lua
function rnet.sit(seat: SeatPart, humanoid: Humanoid)
```

Forces the specified `humanoid` to sit on the given `seat`, creating a weld on the server.

### Parameters
* `seat`: The `SeatPart` to sit on.
* `humanoid`: The humanoid to seat.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local character = player.Character
local seat = workspace.SeatPart

rnet.sit(seat, character.Humanoid) -- Seats the player on the specified seat.
```

---

## rnet.touch

```lua
function rnet.touch(part1: BasePart, part2: BasePart)
```

Simulates a touch interaction between `part1` and `part2`.

### Parameters
* `part1`: The part being touched.
* `part2`: The part initiating the touch.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local character = player.Character
local killbrick = workspace.KillBrick

rnet.touch(killbrick, character.HumanoidRootPart) -- Simulates a touch, potentially killing the character.
```

---

## rnet.unequiptool

```lua
function rnet.unequiptool(tool: Tool)
```

Unequips the specified `tool` from the character.

### Parameters
* `tool`: The tool to unequip.

### Example
```lua
local player = game:GetService("Players").LocalPlayer
local character = player.Character

rnet.unequiptool(character:FindFirstChildOfClass("Tool")) -- Unequips the equipped tool.
```

---

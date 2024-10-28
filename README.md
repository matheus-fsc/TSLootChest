### How the Plugin Works

The TSLootChest plugin allows each player to have individual loot chests on the Terraria map. This means that the loot in each chest is unique to each player, preventing conflicts and duplication.

#### General Functionality
- **Initialization**: The plugin initializes by creating a database to store the location and state of chests for each player.
- **Chest Protection**: Implements logic to prevent players from breaking chests that were not placed by them.
- **Synchronization**: Uses a synchronization object to ensure that operations on chests are thread-safe.

### Available Commands for Players

1. **Add Chest**
   - **Command**: `/addchest <X> <Y>`
   - **Description**: Adds a chest at the specified coordinates if a valid chest exists.
   - **Example**: `/addchest 100 150`

2. **Remove Chest**
   - **Command**: `/remchest <X> <Y>`
   - **Description**: Removes a chest at the specified coordinates if the chest was placed by a player.
   - **Example**: `/remchest 100 150`

3. **Toggle Chest Log**
   - **Command**: `/togglelogchest <true|false>`
   - **Description**: Enables or disables the logging of chest openings.
   - **Example**: `/togglelogchest true`

### Additional Information

- The plugin logs events such as placing and removing chests, ensuring that all actions are recorded and verified.
- To protect chests, the plugin checks if the chest was placed by a player before allowing its removal.

For more details, you can check the code files directly in the repository:
- [Plugin Configuration](https://github.com/matheus-fsc/TSLootChest/blob/main/LootChest/Logicas/Config.cs)
- [Command Definitions](https://github.com/matheus-fsc/TSLootChest/blob/main/LootChest/Logicas/Comandos.cs)
- [Main Plugin Logic](https://github.com/matheus-fsc/TSLootChest/blob/main/LootChest/Logicas/LootChest.cs)
- [Chest Database](https://github.com/matheus-fsc/TSLootChest/blob/main/LootChest/Logicas/ChestDatabase%20.cs)

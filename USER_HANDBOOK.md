# Database Package Console Editor User Manual

## Commands and Usage:
1. Open a File:

   Syntax: open <file_path> [file_nickname]

   Description: Opens the specified resource package file. Optionally, you can provide a file nickname for global command operations. If no file nickname is provided, the source file name without extension will be used.

1. New a File:

   Syntax: new <file_nickname>

   Description: Create a empty resource package file with the nickname you given.
   
2. Close a File:

   Syntax: close <file_nickname>

   Description: Closes the currently opened file associated with the provided file nickname.

3. View an Item:

   Syntax: view <file_nickname> <item_ID>

   Description: Displays the specified item from a resource package file. It will extract and display the item in JSON format for viewing. You will be prompted whether to dump the item.

4. Show File Information:

   Syntax: info <file_nickname>

   Description: Displays index information of the opened file, including items and item metadata. This command will also be automatically executed upon using the "open" command.

5. Search for Item:

   Syntax: search <file_nickname> <partial_item_name>

   Description: Searches for items within the specified file that contain the given partial item name. Displays full item names, metadata, and IDs.

6. Save File:

   Syntax: save <file_nickname> <file_path>

   Description: Saves the opened file to the specified file path.

7. Import Dialogue Tree:

   Syntax: importdialoguetree <file_nickname> <json_file_path> <item_name>

   Description: Imports a dialogue tree from a local JSON file and adds it to the specified item within the opened file.

8. Operate Dialogue Tree:

   Syntax: operatedialoguetree <file_nickname> <item_ID>

   Description: Allows console-based interaction with dialogue items. You can read or edit each dialogue line within the console and save changes.

9. Unpack Image:

   Syntax: unpackimage <image_file_path>

   Description: Converts a packed image file back into a regular image format (e.g., JPG, PNG).

10. Pack Image:

    Syntax: packimage <image_file_path>

    Description: Packs a regular image file into a specialized packed image format.

11. Inflate Index:

    Syntax: inflate <file_path>

    Description: Decompresses the compressed index of a file and writes it out. Useful for those who want to view file indexes with a hex editor.

## Note:
- Double quotes ("") should only be used when the body of a command parameter contains spaces. This helps the command processor correctly separate each parameter. For example, if a file path or project name includes spaces, enclose it in double quotes, like this: open "my folder/my_database.db".
- User responses provided during command interactions (such as confirming a path or input) should not be enclosed in double quotes. These responses are treated as single strings and do not require quotation marks. For example, if asked for a path, provide the path without double quotes: C:\my_files\database.
- Replace placeholders like <file_path>, <file_nickname>, <item_ID>, <partial_item_name>, and <json_file_path> with actual values.
- Square brackets [] denote optional parameters.
- Commands are not case-sensitive.

## Examples:
- To open a file: open "D:\Simulacra\assets\content.fs"
- To search for an item: search main anna
- To view an item: view content 8864
- To import a dialogue tree from a local JSON file: importdialoguetree content "D:\PipeDreams\Dialogues\abc.json" content/chats/introteddy1.DialogueTreeDescriptor
- To save a file: save content "D:\Simulacra\new_assets\content.fs"
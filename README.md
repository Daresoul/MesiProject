# MesiProject Documentation
### Description of the APP
The MesiProject is a combined HTTP server and GUI client application. It features a drag-and-drop interface that allows users to configure server settings and create JSON-based requests for external servers.

The application has two primary components:

* HTTP Server: Processes incoming requests and handles server configurations.
* GUI Client: Allows users to visually build server settings and request payloads using drag-and-drop blocks.

The GUI application is essentially a drag and drop interface to create settings for the server as well as create requests for other servers.

The drag and drop blocks is a request header builder, and a JSON builder. This allows to send json to outbound servers, but also edit the server settings with JSON commands.

On the image below is a test setting, for setting up the server changing different settings in the server and client config file. In the image 4 blocks can be seen. Blue box with 2 text fields, means a key, value pair. The Orange box that can contain other boxes is equivalent to a JSON object. The Gray box that can also contain other blocks, is equivalent to a json array. The last Rosybrown block, is a header field.
![Standard settings image](./DocumentationImages/Screenshot%202024-10-18%20at%2002.14.24.png)

The GUI has been build in Avalonia to ensure cross platform compatability, since I dont know the target OS and the application have only been run on a OSX arm processor (m1).

On the screen you can also start and stop the server, as well as send a Outbound message, or a config message to the server.

The application will outsave when you have moved a block, and released the mouse click.

Besides this there exists a save settings and discard settings button, that will save the position and text of each block in the window, and discard will remove them all giving a clean window.

To add it to the request body you have to add it to the static red body block, and for adding headers it needs to be attached to the green Header button. For sending to our own server a set of standard headers will be used, so the headers are only used for outbound messages.

For the HTTP server, there is a set of different settings that can be set in your request to the server. These are
- **inbound_address:** The IP address on which the HTTP server listens. It can be a specific IP (e.g., ``127.0.0.1``) or ``*`` to listen to any IP.
- **inbound_port:** The port on which the HTTP server listens.
- **outbound_address:** The address to which outbound messages are sent.
- **outbound_port:** The port for outbound messages.
- **path:** Specifies the path for outbound requests (e.g., ``http://example.com/path``).
- **add_header:** A JSON object containing headers to be added to the server’s responses.
- **remove_header:** A JSON object that removes headers from the server’s responses.
- **get_settings:** A special command that fetches the current server settings.


For having the server listen to the correct inbound address and port, if these are changed the server will need a reboot.

The server when changing settings will give you a response in the blue box, as well as if the HTTP server recieves any requests. In the response from changing the settings it will tell you all the updated settings, and if any of them failed to validate, and therefore wont change.

The order of the settings dosent matter and will instead be looked for at the exact same order no matter the order the settings are sent in.

The server listens for setting changes on the path /settings with a POST request expected. The demo application makes no use of security, and therefore any request that the server listens to will be able to change the settings. Such requests could be from Postman or Bruno.

The client only supports sending POST requests at the moment, but likely support for GET and other request types will be added.

### GUI Overview
The GUI allows users to build HTTP requests visually:

* Blue boxes: Key-value pairs for headers.
* Orange boxes: Represent JSON objects and can contain other elements.
* Gray boxes: Represent JSON arrays and can contain multiple elements.
* Rosybrown blocks: Header fields used in the request.

The GUI supports drag-and-drop functionality and autosaves configurations upon mouse release. Buttons for saving and discarding configurations are also provided.

#### Example JSON Request
To change server settings via a POST request to /settings, you can use a JSON body like the following:

```json
{
    "inbound_address": "127.0.0.1",
    "inbound_port": "5050",
    "add_header": {
        "Content-Security-Policy": "default-src 'self'",
        "X-Frame-Options": "DENY"
    }
}
```

### System Requirements
* Operating System: Cross-platform (Windows, macOS, Linux)
* .NET SDK: Version 8.0.403 or higher
* Avalonia: For cross-platform GUI support
* Sqlite: For handling the internal database

### Project Structure
The project contains the following key files and folders:

* DemoMesi.sln: The solution file for the project.
* Client/: The main project folder, containing:
    * Client.csproj: Project file for building the GUI client and HTTP server.
    * App.config: Configuration file with customizable settings for the application.
    * HeaderConfigSection/: Custom configuration for HTTP headers in responses.
    * MainWindow.axaml: The Avalonia XAML file for the GUI layout.
    * bin/: The folder where build artifacts are stored.

### Dependencies
The project makes use of the following dependencies:

* Avalonia: A cross-platform framework for building GUI applications in .NET.
* Microsoft.Data.Sqlite: A lightweight, in-process database engine used to manage HTTP requests.
* System.Configuration.ConfigurationManager: For managing configuration files like App.config.

### Build Instructions
These build instructions have been made using dotnet version 8.0.403 on a macbook pro (m1). So for best compatability ensure that the dotnet version used is version 8.0.403. For these instructions we will make use of the dotnet cli.

Navigate to the repository folder and go

```
cd DemoMesi
```

If it is the first time, and the config file havent been set yet, you will need to set the config file. I havent included a config file in case the config will include secrets in the future. To do so you simply copy the "App.template.config" and rename it to App.config and it should contain all necesarry fields with default values for now.

```
cp App.template.config App.config
```

NOTE: If you need to do it on the live program you have to find the program folder, (debug build is in the project folder in the bin folder) and open "Client.dll.config" in a text editor.

In the folder ensure to run the restore command to gather all of the dependencies.

```
dotnet restore DemoMesi.sln
```

From here to build it you can simply run the build command.

```
dotnet build DemoMesi.sln
```

You can then run the application using the run command.

```
dotnet run --project Client/Client.csproj
```

If you want to run it as a release it its own folder, you can use the publish comamnd below, which will put the program into a folder in the git root folder.

```
dotnet publish Client/Client.csproj -c Release -o ../published/
```

### Further work

Add security headers for chaning HTTP server settings.

More validation in the HTTP server to ensure flawless handling of all requests.

Giving the HTTP server a reason to listen to other requests than from the Client.

Add request type to the GUI so we can send other than POST requests.

Support for deleting a single block will be added, as of now it can only be moved out of the request or delete the entire GUI config.

Validation of the JSON that is sent, with errors if it cant validate.


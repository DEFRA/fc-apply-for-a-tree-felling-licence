# API JS Widgets

**Note:** You shouldn't need to touch these files, The system doesn't use any of the VM classes to simplfy the use of the tools 

Esri Widgets was how the Orginal JS API was built. 
The approach Used AMD and MVC approaches. In Later versions of the API ESRI have started the move to ES modules.
This transistion is in progress and thus AMD is still supported. 

But the core idea remains: a widget is thought of as a piece of the API that encapsulates a specific set of functionality. The API provides ready-to-use widgets with predefined functionality.

## Getting Going
Visual Code is my recomended IDE for this:
 - Open the folder GIS Tools.
 - Create a folder called "outDir"
 - run the command 
`npm install` - This will install all NPM packages needed to build the application
- Build as per normal. 
This will generate JS files based on the config given in the TSConfig file. Then simply copy the Generated JS Files into the Application 

### Other notes:
 - The Files use TSX, TSX Files are the same as JSX (Javascript XML, using the REACT UI and application approach) - but in Typescript
 - [ESRI Notes](https://developers.arcgis.com/javascript/latest/sample-code/widgets-custom-widget/)

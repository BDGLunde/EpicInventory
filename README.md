# EpicInventory

This project was used as the culmination of Epic's week-long intensive C#/.NET training camp. It demonstrates various core C#and OOP concepts and usage of basic .NET components, so in light of not being able to share code for other small internal projects I worked on during my time at Epic, I figured <strong>this would serve as a demo/sample of a subset of my experience with C#/.NET.</strong>

The basic idea of the project is to simulate an inventory system that supports (de)serialization for persistence as well as event handling for keeping total inventory statistics up to date when the properties of a single item type is changed by the user. The user interface is a simple console/text application.

Before committing this project, I added support for being able to press the [ESC] key to navigate backwards through prompts within a menu and back through menus themselves. 

No "skeleton code" was provided, and everything besides the "...SuppliedCode.dll" (which mostly provided Interfaces to be adhered to and instrumented collections for unit tests) and most of the unit tests (and of course, the .NET components themselves :)) were written by me.

*.suo (Visual Studio user option extension) files were included in the repo so that the correct Startup project for the solution is maintained when cloned. 

I do not plan on updating this project any further. 

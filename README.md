# SolidWorksProjectSwitcher
Speeds up your workflow by allowing you to quickly change your current project in SolidWorks.

##Requirements
* Microsoft .NET Framework 4.5.2

##Installation
Either compile the program from source or download an already compiled version [here](https://download.stefanfabian.me/Programs/SolidWorksProjectSwitcher/SolidWorksProjectSwitcher.zip)  
The compiled version comes as a ready to use zip file that you can extract and run anywhere.  
**No installation required!**

Alternatively you can also install a [ClickOnce version](https://download.stefanfabian.me/Programs/SolidWorksProjectSwitcher/ClickOnce/publish.htm).  
ClickOnce applications are installed from a webserver (although you can also install them offline from CD/DVD etc.), automatically update themselves and do not require administrator privileges to install or run.  
Check [Wikipedia](https://en.wikipedia.org/wiki/ClickOnce) if you want to know more.  
Since I can't afford to sign the application, you'll have to click *More information* -> *Install anyway* if a Windows warning pops up.

##Set-Up
Change the path in **solidworkspath.ini** to the path of your SolidWorks executable.  
Make sure the file only contains the path and no leading or trailing whitespaces or newlines.  
**Specify the path using backslashes as separator!**  
For example:
  
*C:\Program Files\SolidWorksPath\SldWorks.exe*  
  
Now update the process name in **solidworksprocessname.ini**.  
The default is:
  
*SLDWORKS*

**Sidenote:** The case is in this case ignored, so sldworks would work as well.  
**This only applies to the solidworksprocessname.ini!**

And finally, change the value in **solidworksprojectfolder.ini** to the folder you usually use for your projects  
**It is very important to use only a single backslash as separator!**  
For example:
####Right
*C:\solidworksproject*
####Wrong
~~*C:\\\\solidworksproject*~~  
~~*C:/solidworksproject*~~

##How does it work

![Image of the application][demoimage]

The entries in the *Other SolidWorks projects* are folders that start with the path set in **solidworksprojectfolder.ini*.  
To give you a better impression here's the folder structure on the hard drive.

| Real folder | Entry |
| ----------- | ----- |
| C:\solidworksproject | _ImportantProject (Current project) |
| C:\solidworksproject_AnotherImportantProject | _AnotherImportantProject |
| C:\solidworksproject_prefix_ProjectWithLongerPrefixAsExmaple | _ProjectWithLongerPrefixAsExample |

The name of the project in *C:\solidworksproject* is known because the folder contains a hidden file named **name.solidworksprojectswitcher.ini**.  
This file is updated whenever you rename a folder that starts with your project folder path.  

##Usage

Select the project you want to switch to and click **Switch**  to switch to that project.  
If you were working on another project before, it will ask you to enter a name for the previous project.
The names are saved in a special ini file in the project folders. So, in most cases the application will know the previous name and suggest it.  
The folders will be renamed to the name of your project folder as set in **solidworksprojectfolder.ini** + the project name.
**Example:**  
If your SolidWorks project folder is '*C:\solidworksproject*' and the name of your current project is '*_ImportantProject*' it will be renamed to:  
*C:\solidworksproject_ImportantProject*  

You can also delete the selected entry by clicking on **Delete**

If your current project was just a quick test and you do not wish to keep it, click **Switch and delete current** to delete the current project folder and switch to the selected entry.

##Settings
Check **Start SolidWorks after switch** if you want SolidWorks to be started automatically after the project folder was switched.  
This requires the correct path to the SolidWorks executable to be set in the **solidworkspath.ini**

Name-prefix is an optional prefix that is suggested whenever the name of the current project is unknown.
As seen in the following image, it is also not marked when renaming to further improve your workflow.

![Image of the renaming popup][renamedemoimage]

##Optional

If you found a bug, have a question or whatever else you come up with, you can send me a mail to  
*me at stefanfabian dot me*  
Alternatively I have a [contact form](https://stefanfabian.me/contact) on my homepage.

If you for some reason need the good feeling of having pressed a button that caused your bank or paypal to send some money to someone else which is commonly referred to as donation, I won't keep you from doing that.  
It's completely optional though and you don't have to feel bad if you don't.

[![](https://www.paypal.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=38X6FHSQ3WZAS)


[demoimage]: https://github.com/StefanFabian/SolidWorksProjectSwitcher/raw/master/Images/demo.png
[renamedemoimage]: https://github.com/StefanFabian/SolidWorksProjectSwitcher/raw/master/Images/rename-demo.png

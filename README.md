# Oldschool.I18n

# What is this?

It is a .NET library that finds localizable messages in C# and F# source code and stores them in JSON resource files updating it if possible. 

# Why would I like to use it?
Shortly - because [resources based approach](https://www.codeproject.com/Articles/778040/Beginners-Tutorial-on-Globalization-and-Localizati) is less readable.

## _"It's just an opinion, convince me with arguments"_

### Resource based way...

Assuming you need to have message needing localization in your source code:

```csharp
	return string.Format("Foo is {0} and is bigger than planned {1}. Are you sure?", someVar, actual);
```

This means that you need to add to your resources:
```FooIs0AndIsBiggerThanPlanned1AreYouSure = Foo is {0} and is bigger than planned {1}. Are you sure?```

and so in sources you have now:

```csharp
	return string.Format(MyRes.Resources.FooIs0AndIsBiggerThanPlanned1AreYouSure, someVar, actual));
```

This is _not really_ readable anymore. 

### ... and alternative way
Now what is the alternative? Using Oldschool.I18n you can write it in a kind-of-similar fashion as in [GNU gettext](https://en.wikipedia.org/wiki/Gettext). In your source code readable string stays as it was, it is just wrapped into I18n.Translate() call.

```csharp
return string.Format(I18n.Translate("Foo is {0} and is bigger than planned {1}. Are you sure?"), someVar, actual);
```

# How it works

It scans source code to find somethings that look like invocation of someI18nClass someI18nMethod that is present outside of comments.
Then it loads existing translation in JSON format (if there's any) and merges them together. During merge it discards not needed translations and adds new ones. Output JSON looks like follows:

```javascript
{
  "items": [
    {
      "m": "extracted English message",
      "t": "your provided localized message",
      "at": []
    },
	...
}
```

or depending on verbosity setting:

```javascript
{
  "items": [
    {
      "m": "extracted English message",
      "t": "your provided localized message",
      "at": [
        "Relative dir of sources\\Source file name.cs:51",
        "Relative dir of sources\\Source file name.cs:98"
	  ]
    },
	...
}
```

# How to install it?

Download it and compile it OR take it from nuget
```
Install-Package Oldschool.I18n
``` 

# How to use it?

Simpliest use is to add it via nuget and then create batch file in root folder of your solution with following content

```
cd packages\OldSchool.I18n*\tools\net40
OldSchool.I18n.Tool.exe -c I18n -m Translate -o ..\..\..\..\translation_ZZZZZ.json -q -d ..\..\..\..
```

where ZZZZZ will typically be CultureInfo's name such as pl-PL

Supported command line parameters:
 * -c NameOfI18nClass
 * -m NameOfI18nMethod
 * -o pathToOutputJsonFile
  created or updated depending on its existence
 * -q  
 Include empty 'at' attribute in JSON. You won't see translation file as changed in your Version Control System if just a location of messages changed.
 'At' section may be usefull if you need to identify message origin.
 * -d directoryToScan
 Adds directory with sources to be recoursively scanned
 
# Is it stable?
I think so. There are plenty of tests written for both C&#35; and F&#35;.

# Features (which constructs are supported?)

 * Ignores single line and multiline comments  
 in C&#35; ```//this part is ignored```  
 in C&#35; ```/* this part is ignored */```  
 in F&#35; ```//this part is ignored```  
 in F&#35; ```(* this part is ignored *)```  
 in F&#35; it understands that ```(*)``` is not a beginning of comment
 * Permits whitespace between class and method and its parameter  
 in C&#35; and F&#35; ```I18n    .Translate ( "something") ```
 * Permits verbatim and nonverbatim strings
 in C&#35; ```I18n.Translate(@"something")```  
 in F&#35; ```I18n.Translate("""something""")```
 * Understands escaping  
 it properly understands ```\t``` and likes in both verbatim and nonverbatim strings
 * Permits invocation via currying in F&#35;   
 ```I18n.Translate "something"```

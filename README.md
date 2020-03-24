# OldSchool.I18n

![.NET Core](https://github.com/d-p-y/oldschool-i18n/workflows/.NET%20Core/badge.svg)

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
Now what is the alternative? Using OldSchool.I18n you can write it in a kind-of-similar fashion as in [GNU gettext](https://en.wikipedia.org/wiki/Gettext). In your source code readable string stays as it was, it is just wrapped into I18n.Translate() call.

```csharp
return string.Format(I18n.Translate("Foo is {0} and is bigger than planned {1}. Are you sure?"), someVar, actual);
```

# How it works

It scans source code to find somethings that look like invocation of someI18nClass someI18nMethod(s) that is present outside of comments.
Then it loads existing translation in JSON format (if there's any) and merges them together. During merge it discards not needed translations and adds new ones. Output JSON looks like follows:

```javascript
{
  "items": [
    {
      "m": "extracted English message",
      "t": "your provided localized message"
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

Download it and compile it OR install dotnet core and then invoke 
```
dotnet tool install --global dotnet-oldschool-i18n
``` 

If you decide to uninstall it later then use: 
```
dotnet tool uninstall --global dotnet-oldschool-i18n
```

# How to use it?

Once installed as global tool it can be invoked anywhere

```
dotnet-oldschool-i18n -c I18n -m Translate -o translation_ZZZZZ.json -d .
```

where ZZZZZ will typically be CultureInfo's name such as pl-PL

Supported command line parameters:
 * -c NameOfI18nClass
 * -m NameOfI18nMethod
 NOTE: this parameter may be specified multiple times making the tool look for several methods in source code
 * -o pathToOutputJsonFile  
  created or updated depending on its existence
 * -q  
 Include 'at' attribute in JSON. Thanks to that you won't see translation file as changed in your Version Control System if just a location of messages changed.
 'At' section may be useful in case need to identify message origin.
 * -d directoryToScan  
 Directory with sources to scan recursively
 NOTE: this parameter may be specified multiple times making the tool look into several folders
 
# Is it stable?
I think so. There are plenty of tests written for both C&#35; and F&#35;. In my company we are using it in production for several years.

# Features (which constructs are supported?)
 * C# support uses Microsoft.CodeAnalysis.CSharp
   Permitted invocations are of kind ```NameOfI18nClass.NameOfI18nMethod(one-or-more-parameter-where-only-first-is-used-and-has-to-be-string)```
   Assuming that command line parameters are: ```-c I18n -m Translate``` 
   it will find all the following: 
   ```I18n.Translate(@"Something")``` 
   ```I18n.Translate("Something else")```
   ```I18n.Translate("Another one {0} {1}", 1, 2)```
   ```I18n.Translate("Another one {0} {1}").SomeExtensionMethod(1, 2)```

 * F# still uses "dirty hack" parsing 
   * Ignores single line and multiline comments  
 in F&#35; ```//this part is ignored```  
 in F&#35; ```(* this part is ignored *)```  
 in F&#35; it understands that ```(*)``` is not a beginning of comment
   * Permits whitespace between class and method and its parameter  
  ```I18n    .Translate ( "something") ```
   * Permits verbatim and nonverbatim strings
 in F&#35; ```I18n.Translate("""something""")```
   * Understands escaping  
 it properly understands ```\t``` and likes in both verbatim and nonverbatim strings
   * Permits invocation via currying in F&#35;   
 ```I18n.Translate "something"```

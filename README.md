# Markerator
A very simple static website generator written in C#/.Net. I created this mainly because I was disappointed at the sheer complexity of any of the existing static site generators I was able to find. No offense to anyone creating web content, but to me, things have gotten far too complex to create even an MVP website. I plan to dogfood markerator for my own website once it reaches a minimum level of maturity. And yes, I realize the logo is very immature.

<div style="text-align: center;">
![Markerator Logo](docs/images/markerator_logo_small.png)
</div>

# Simple
Although it seems to be a necessary evil nowadays, I tend to detest javascript use in web pages. That being said, the sites generated using Markerator will use no/minimal javascript, and will offer no tracking or analytics features built-in. If that's what you need in a static site generator, feel free to fork this repo and add those things yourself - I'll never include them by default.

# Platforms
Any platform that .NET 5 or newer supports.

## Usage
```
usage: markerator -h
A static website generator written in C#.

-t|--title       The title of the website. Examples: Markerator Generated Site, 
                 zombo.com
-i|--indexFile   The markdown file that is to be converted into the index.html 
                 file. Examples: mainFile.md, radicalText.md
-p|--posts       Optional with default 'False'. Whether or not the site should 
                 include a posts link (like a news or updates section.) 
                 Examples: true, false
-pt|--postsTitle Optional with default 'Posts'. The title that the posts 
                 section should use. Examples: News, Updates, Blog
-f|--favicon     Optional with default 'False'. Whether or not the site should 
                 use a favicon.ico file in the /input/images directory. 
                 Examples: true, false
-c|--css         Optional with default ''. Inlude custom CSS file that will 
                 theme the generated site. Examples: LightTheme.css, 
                 DarkTheme.css
```

## Examples
In order for markerator to correctly process your markdown input files, it expects a pre-determined folder layout, with a number of folders/files inside it. It will then process the input files and if successful put the result Html site into the `/output` folder.

### Minimal Site
Below represents the minimum that markerator expects in order to create a valid Html website.
#### Folder Layout
```
/input/index.md
/markerator
```

#### Commandline
```
./markerator --title Bengineering --indexFile index.md
```

### Site including Posts and a favicon
Example including a posts section and a favicon.
#### Folder Layout
```
/input/images/favicon.ico
/input/posts/post1.md
/input/posts/post2.md
/input/index.md
/markerator
```

#### Commandline
```
./markerator --title Bengineering --indexFile index.md --favicon true -posts true
```

## Features
- [x] Markdown input processing (via Markdig)
- [x] Html interception/injection (via HtmlAgilityPack)
- [x] CLI argument processing (via FluentArgs)
- [x] Post Generation

## Todo
- [ ] Custom Css loading
- [ ] Theme selection
- [ ] A few sensible included themes
- [ ] Image manipulation for Post Summary (cards)
- [ ] Page footers
- [ ] Multiple Page creation for more than just index.html and Posts
- [ ] Better exception/error handling and user direction
- [ ] Include markerator in Github CI
- [ ] App versioning
# Markerator
A very simple static website generator written in C#/.Net. I created this mainly because I was disappointed at the sheer complexity of any of the existing static site generators I was able to find. No offense to anyone creating web content, but to me, things have gotten far too complex to create even an MVP website. I plan to dogfood markerator for my own website once it reaches a minimum level of maturity. And yes, I realize the logo is very immature.

![Markerator Logo](docs/images/markerator_logo_small.png)

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
./markerator --title Bengineering --indexFile index.md --favicon true
```

### Site including Posts and a favicon
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

# Features
- [x] Markdown input processing (via Markdig)
- [x] Html interception/injection (via HtmlAgilityPack)
- [x] CLI argument processing (via FLuentArgs)
- [x] Post Generation

# Todo
- [ ] Custom Css loading
- [ ] Theme selection
- [ ] A few sensible included themes
- [ ] Image manipulation for Post Summary (cards)
- [ ] Multiple Page creation for more than just index.html and Posts
- [ ] Better exception/error handling and user direction
- [ ] Include markerator in Github CI
- [ ] App versioning
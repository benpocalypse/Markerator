# Markerator
A very simple static website generator written in C#/.Net. I created this mainly because I was disappointed at the sheer complexity of any of the existing static site generators I was able to find. No offense to anyone creating web content, but to me, things have gotten far too complex to create even a minimal website. I plan to dogfood Markerator for my own website once it reaches a minimum level of maturity. And yes, I realize the logo is very immature. I'll get a better one some day.

![Markerator Logo](docs/images/markerator_logo_small.png)

## Interim Icon
I've asked a very talented artist to create a good icon for Markerator that complies with the Gnome HIG/style, but in the meantime, I've come up with this as a placeholder.

![Markerator Icon](Icon.png)


# Simple
Although it seems to be a necessary evil nowadays, I tend to detest javascript use in web pages. That being said, the sites generated using Markerator will use no/minimal javascript, and will offer no tracking or analytics features built-in. If that's what you need in a static site generator, feel free to fork this repo and add those things yourself - I'll never include them by default.

# Platforms
Any platform that .NET 5 or newer supports.

## Usage
```
usage: markerator -h
A very simple static website generator written in C#/.Net

-t|--title       The title of the website. Examples: Markerator Generated Site, 
                 zombo.com
-u|--url         The base Url of the website, omitting the trailing slash. 
                 Examples: https://www.slashdot.org, https://elementary.io
-i|--indexFile   The markdown file that is to be converted into the index.html 
                 file. Examples: mainFile.md, radicalText.md
-p|--posts       Optional with default 'False'. Whether or not the site should 
                 include a posts link (like a news or updates section.) 
                 Examples: true, false
-pt|--postsTitle Optional with default ''. A single title, or comma separated 
                 list of titles, that represents a link to each section of the 
                 site that will be a 'feed.' Each postsTitle specified should 
                 have a corresponding folder that contains one or more Markdown 
                 files. Examples: News, Updates, Blog, ProjectsMultiple values 
                 can be used by joining them with any of the following 
                 separators: , ;
-rss|--rssFeed   Optional with default 'False'. Whether or not to generate Rss 
                 feeds from your posts/news/blog pages. Examples: true, false
-ri|--rssIcon    Optional with default 'False'. If set to true, and an icon 
                 named 'rss.jpg' or 'rss.png' exists in the /images folder, 
                 then an icon link will be created that links to each pages Rss 
                 feed. Examples: true, false
-f|--favicon     Optional with default 'False'. Whether or not the site should 
                 use a favicon.ico file in the /input/images directory. 
                 Examples: true, false
-op|--otherPages Optional with default ''. Additional pages that should be 
                 linked from the navigation bar, provided as a comma separated 
                 list of .md files. Examples: About.md,Contact.mdMultiple 
                 values can be used by joining them with any of the following 
                 separators: , ;
-c|--css         Optional with default ''. Inlude a custom CSS file that will 
                 theme the generated site. Examples: Blue.css, 
                 DarkTheme.css 
-pp|--postsPerPage Optional with default 0. This will tell Markerator how many posts should appear on the News/Updates/Blogs landing page. Specifying 0 will mean there is no limit, specifying any other number will create a series of pages that are linked to each other for pagination.
```

## Examples
In order for Markerator to correctly process your markdown input files, it expects a pre-determined folder layout, with a number of folders/files inside it. It will then process the input files and if successful put the resulting Html site into the `/output` folder.

### Minimal Site
Below represents the minimum that markerator expects in order to create a valid Html website.

#### Folder Layout
```
/input/index.md
/markerator
```

#### Commandline
```
./markerator --title Geocities --indexFile index.md
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
./markerator --title Geocities --indexFile index.md --favicon true --posts true
```

### A note about Posts
As of the 0.2.5 version of Markerator, if your side includes Posts/News/Blog/Projects markdown files, there is a new option that can be used. If the entry in your markdown file is of headine H1, and is formatted to contain any valid `DateTime` in it, then Markerator will generate a Posts/News/Blog/Projects page for you that organizes the posts by their given dates. If this H1 section is ommitted, then Markerator will just categorize Posts/News as "All" in that section of the generated site.

For example, if the top of your Posts/News/Blog/Projects entry starts like the following:

```
# 08/29/1997

## Skynet Sentience

We never knew this day would come, and yet it did. 
```

This will produce an entry on the overview Posts/News/Blog/Projects page that will sort this entry by date, and provide a header of "Skynet Sentience" With the first line being the "summary" for that entry.

## CSS File Documentation
The specific format for a custom CSS file that Markerator can use to style your site is documented here: [CSS File Format](docs/CssFormat.md) 

Although this intended to be a stable document when v1.0.0 is released, there may be breaking changes in the future. If that is the case, it will be made obvious in this Readme file, and the historical CSS Format that goes with each previous version will remain active and noted here.


#### Known Bugs
- [x] ~~At the moment, the Posts/News section is sorted internally by File Creation Time, so even if they contain valid and ordered `DateTime` values, they will still be listed by File Creation Date in the generated Posts/News section. This should be addressed in a following update.~~
- [ ] An attempt was made using CSS and HTML to make the Markerator generated sites responsive for mobile use. However, there is a bug that makes pages that use Markdown Tables behave oddly. This is being looked into and will be addressed in a future Markerator release, and noted here. For now, the CSS Format Documentation will just omit this feature.

## Features
- [x] Markdown input processing (via Markdig)
- [x] Html interception/injection (via HtmlAgilityPack)
- [x] CLI argument processing (via FluentArgs)
- [x] Post Generation
- [x] App versioning
- [x] Custom fonts
- [x] Multiple Page creation for more than just index.html and Posts
- [x] Custom Css loading
	- [x] Basic Css validation
- [x] Page footers
- [x] Include markerator in Github CI
- [x] Add RSS feed support 
	- [x] Rss icon is now shown on the Posts/News/Blog/Projects pages, linking to the Rss feed
- [x] Refactor the code to map different page types to different concrete classes. Simply manipulating strings has gotten out of hand.
- [x] Refactor the code to move the Html generation out of markerator.cs
- [x] Abstract out the "posts" concept to allow any number of links in the navigation that lead to a posts-style page
- [x] Add Open Graph support to generated pages to allow unfurling of URL's on various social media sites
- [x] Add user-definable pagination to Posts/News/Updates pages
- [x] Theme selection - custom CSS files can now be specified as an argument
- [x] A few sensible included themes - Markerator now ships with Light and Dark variants of the default theme, as well as Light and Dark variants of a Blue theme, and 2 more additional themes.
- [x] Advanced CSS validation
- [x] Refactor the code to allow for output directory cleanups, and move the directory code into it's own class
- [x] Add a toggle for light/dark mode to generated sites using only pure HTML/CSS
- [x] Better exception/error handling and user feedback - use StdOut and StdError
- [x] Document the theme CSS File Fformat to allow users to create new themes for Markerator

## Todo
- [ ] Update documentation to be more robust and explicit
- [ ] Create a guide to walk through how to set up Github automation and host a Markerator site on Github
- [ ] Per page/post Open Graph images, rather than just a single site-wide Open Graph image
- [ ] Image manipulation for Post Summary (cards)
- [ ] Custom footers  
 themes
 
# Credits
Markerator uses a number of very handy nuget packages to do what it does, and for that, I am very thankful. Please help support the authors where you can, as they're doing everyone a great service:

* [FluentResult](https://github.com/altmann/FluentResults) - Exception handling shouldn't be control flow.
* [FluentArgs](https://github.com/kutoga/FluentArgs) - The best CLI argument handling library around.
* [FluentAssertions](https://github.com/fluentassertions/fluentassertions) - A great library that makes Unit Tests much more readable and easy to write.
* [Markdig](https://github.com/xoofx/markdig) - Converting markdown to Html couldn't be easier.
* [HtmlAgilityPack](https://github.com/zzzprojects/html-agility-pack) - Peeking at Html made dead simple.
* [ExCss](https://github.com/TylerBrinks/ExCSS) - Peeking at Css made dead simple.


# Markerator
A simple static website generator written in C#/.Net.


```
usage: markerator -h
A static website generator written in C#.

-t|--title       The title of the website. Examples: Markerator Generated Site, 
                 zombo.com
-i|--indexFile   The markdown file that is to be converted to the index.html 
                 file. Examples: mainFile.md, radicalText.md
-p|--posts       Whether or not the site should include a posts link (like a 
                 news or updates section.) Examples: true, false
-pt|--postsTitle Optional with default 'Posts'. The title that the posts 
                 section should use. Examples: News, Updates, Blog
-f|--favicon     Optional with default 'False'. Whether or not the site should 
                 use a favicon file in the images directory. Examples: true, 
                 false

```
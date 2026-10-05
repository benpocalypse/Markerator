# 02/17/2024

## Rss Has Landed!

After much head scratching on thinking of a "clean" way to implement Rss feed generation in Markerator, I've finally pushed the code to GitHub that does it. If anyone is actually using Markerator (and I doubt it) the Readme has been updated to indicate how to use this new functionality.

I think the next big thing I'm going to tackle is adding the ability to generate multiple "posts" pages at the same time. So you could have "Projects," "News," and "Journal" all being generated at the same time, each one with it's own Rss feed generation. I also need to link to the Rss file in the actual generated website, but that will come in the future. 

Update: There was a bug in the code I pushed that had the "postsTitle" hardcoded to "posts" in the index.html generation, so even though Markerator generated things correctly, it wouldn't link to them correctly. 

Update 2: Make that 2 hardcoded bugs ¯\_(ツ)_/¯

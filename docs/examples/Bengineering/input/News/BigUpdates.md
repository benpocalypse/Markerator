# 10/01/2026

## Finally, some big Markerator Updates
I spent a bunch of hours over the last few days finally giving the Markerator codebase the love it deserves. The version I just pushed includes the following changes:

 - Massive refactor and cleanup of the existing code (this was a long time coming.)
 - More code broken out into `Helper` classes to declutter existing purpose driven classes
 - Unit Tests written for all of the `Helper` classes, as well as some. rudimentary tests for the main Markerator entry point
 - Some changes to the Open Graph support.
 - User specified pagination for Posts/News/Updates sections. This allows a user to break the overview page by the specified level of pagination. For example, if you have 25 Posts, and you specify a pagination of 10, you'd end up with 3  pages that are linked together showing 10 posts per page.

My next big push is going to be for some new CSS themes, as well as documentation of the current CSS format to allow for easier end user CSS theme creation. Onward towards v1.0.0!

UPDATE: There was a bug in yesterday's Markerator commit that broke CSS handling. I've fixed this and added more unit tests to verify the functionality. If a user doesn't specify a CSS file, the default will be used.

UPDATE 2: There are still more bugs. This is starting to get complicated. Hopefully this round of changes takes care of them. Maybe not though...

UPDATE 3: Third times a charm, right? Yeah, no.

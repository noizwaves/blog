---
title:  Regex Find and Replace in IntelliJ
date:   2017-07-15 12:01:43 -0600
---

The other day I was preparing a CSV file for load into a PostgreSQL database.
The source file contained a datetime column in an unusual format (`'15JUL2017:00:00:00'`), which needed to be transformed to a compatible format (`'15-JUL-2017:00:00:00'`) before loading with the `\copy` CLI command.
Values representing many different days, months, and years were present in the thousands.

![CSV Data]({{ site.url }}/assets/intellij-regex/csv.png)

My usual IntelliJ tricks did not work.
The file was large (4000 lines), which meant batch editing with multiple carets by matching all occurrences would have been too slow.  
The pattern itself was easily findable by a regex (2 numeric characters, 3 non-numeric characters, 4 numeric characters), but the replacement string depends on the groups matched.
 
Turns out IntelliJ has a feature to deal with this.
In the replacement string, you can simply reference the match group via `$X` (where `X` is the group number, starting at 1).

![Regex Find and Replace]({{ site.url }}/assets/intellij-regex/solution.png)

In the end, a search regex of `(\d{2})(\D{3})(\d{4})` and a replacement string of `$0-$1-$2` can be used.
IntelliJ is full of useful features.
Hopefully this will come in handy for you one day.

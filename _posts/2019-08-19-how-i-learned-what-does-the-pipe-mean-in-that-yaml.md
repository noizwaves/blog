---
title:  How I Learned "What does the pipe mean in that YAML?"
date:   2019-08-19 9:39:00 -0600
---

In this post, I want to talk about a particular experience on a recent project.
It started when my pair asked me the straight forward question "What does the pipe mean in that YAML?".

I'll start with by going over my initial response, then how I caught myself being fearful and prideful, and then how I changed my response to one of honesty and vulnerability.
After that, I'll touch on why this all matters.
Finally, I'll wrap with a summary of some other approaches I use when responding to questions/problems when pairing.

## TLDR - What does the pipe mean in that YAML?

It's all about how new lines are treated in multi-line strings.
This is called **block style**.

When defining a multiline string, if the new lines should:

-   remain as new lines, use a `|` (this is "literal" block style)
-   be replaced with spaces, use a `>` (this is "folded" block style)

Take a look at this example of a _literal block style_. You can see the output contains **2** new line characters.
```javascript
> yaml.parse("foo: |\n  bar\n  baz")
{ foo: 'bar\nbaz\n' }
```

Now contrast this with the _folded block style_ alternative. Note how the output only contains **1** new line character.
```javascript
> yaml.parse("foo: >\n  bar\n  baz")
{ foo: 'bar baz\n' }
```

There is more to learn too.
I'd recommend you visit [YAML Multiline Info](https://yaml-multiline.info) if you want to learn more.
It does a great job at exploring multiline strings in YAML.

## Welcome

If you came here to learn about pipe symbols in YAML, the answer can be found above.
Good luck on your multiline strings in YAML journey.
I'd still encourage you to read on though.

If you came here to learn __how__ I learned about pipe symbols in YAML, the rest of this post is for you.
I really do hope you continue reading.
This post is all about the "how".
It's my hope that the "how" contains some useful knowledge that is generally applicable to you (a technical professional) and can help you grow.

## It started with a question

Recently I was working on a engagement to install Pivotal Cloud Foundry (PCF) for a customer.
I was pairing with a client engineer and we were editing some YAML configuration files.
This is all pretty standard.

Then my pair asks me the question:
> What does that pipe symbol mean in that YAML?

They are using the cursor to point at a `|` character that precedes a multiline string containing an SSH private key.
I aim to cultivate a safe and trusting environment in pairing sessions, with the intention that that "any question is welcome".
And this is a really good question.
It's really good for a couple of reasons:

1.  it's related to our current task (not a tangent) which demonstrates they are engaged and thinking
1.  challenges the mental model we had been building about what YAML is (keys, values, nesting) up until now

## The first answer

My answer was quick to form, and was something like:

> I think it has to do with new line characters...

This was a gut reaction.
What I didn't say (and was definitely thinking) was that this answer was formed through assumptions and guesses made through my career as a technical professional.
In reality the pipe symbol could be doing something completely different, and I didn't actually _know_.

So in general, I'm a fairly introspective person.
After I gave that answer, I caught myself pretty quickly, because I realized that answer was not good enough.
We deserve better than my limited understanding and perpetuating my assumptions to other people.
I could do better as a technical professional/expert/teacher.
This could (and should) be an enablement moment.

## A better answer

Maybe it's because I started re-reading Extreme Programming Explained recently, and the values of courage and honesty were fresh in my mind.
After a little thought, this actually seemed like an excellent opportunity to demonstrate that it's OK to admit not knowing something.

So then I said something like:

> You know what, I'm not actually 100% sure. I've seen the pipes in a lot of YAML, but never really looked for the meaning. I want to find out tonight and tell you tomorrow.

And this is exactly what I did!

After getting home and doing a quick bit of Googling, I stumbled across some good Stack Overflow posts ([1](https://stackoverflow.com/questions/3790454/how-do-i-break-a-string-over-multiple-lines) and [2](https://stackoverflow.com/questions/15540635/what-is-the-use-of-the-pipe-symbol-in-yaml)),
and then ultimately [YAML Multiline Info](https://yaml-multiline.info/).
After learning the answer, I couldn't wait until the next day to share, so I sent a quick Slack message with some links to my pair.

## Why the better answer matters

This answer was much better, and I was a lot happier with it.
Instead of propagating assumptions, I like to think that:

1.  we both learned the actual technical meaning of pipes in YAML
1.  we also learned there is a lot more to multiline strings in YAML
1.  I was able to demonstrate, especially as a "expert", it's OK to not know something (and you won't get fired)
1.  we are both smarter and more capable than before
1.  our pairing environment is even more safe and trusting than before

Not knowing things is inevitable in our field because:

1.  it is vast
1.  it changes continually (and rapidly)
1.  our time is finite
1.  we forget things

## Appendix A: My toolbox of approaches

Faced with this same situation in the future, I'd likely use the same approach (let's call it "learn later and share").
We were short of time, mainly due to spending a bunch of time trying to fix some other systems.
"Learn later and share" works great if you are short of time but still want to teach.

"Learn later and share" is just one of the approaches in my toolbox when pairing.
Below is a list of other approaches I regularly use to great success.

### "find out together"

The first approach is "find out together", and like the name suggests you literally learn together.

You start with some kind of question or problem - the best is an error message or stack trace.
You, guiding your pair, read through the error.
This means point out the keywords, important information, and distracting noise as you see it.

Then, as a pair, you start on finding solutions.
This could mean doing a Google search, going directly to the documentation/public APIs, or digging through source code.
Like all things pairing, it's important to verbalize your thinking each step of the way.

Likely you'll have to iterate here based on feedback.
Maybe your first search results sucked, and you need to try better keywords; explicitly talk about this.
Maybe you find a solution and solve this problem, only to be presented with another error!

This approach is one of my favourites because, in addition to demonstrating it's OK to not know something **and** then finding it out, you are able to demonstrate _how_ to find answers.
Essentially, this is "Give a Man a Fish, and You Feed Him for a Day. Teach a Man To Fish, and You Feed Him for a Lifetime".

### "review the tools"

Another approach is "review the tools".
It's pretty straight forward; you go over all the "tools" available to see which one helps.
Tools can be CLIs and *nix commands, or even other things like design patterns, language primitives, external servers.
The tools really depend on your context.

Start by explicitly stating what you are trying to do.
This could be "we are trying to get the admin credentials for our sandbox environment" or "we are trying to tail the logs on the unhealthy process".

Then, perform a guided review of all the tools that are available.
It helps if you already have a list of all available tools.
If this list doesn't exist yet, write one out on a whiteboard or piece of paper.
Start at the top of the list, and go through line by line, and talk about if this tool can help.
You may have to do some guiding here.

When you get to one that is suitable, you may have to repeat the process with the chosen tool.
This is common for CLIs, which may have many commands each with many arguments.
I'll always make a point of printing the help via `some-cli -h` or `some-cli --help` to display a list of the commands or arguments.
This makes discovering answers so much more likely, plus it's a habit that can effectively help to solve future problems.

### "drop some hints"

If a question or problem has already come up, another approach is to "drop some hints".
Here, you guide the conversation to the fact this is similar to something seen before.

It may not be clear that this problem has been seen and solved already.
Provide some gentle guidance back to the last time this came up.

If it's not the exact same problem, provide guidance to the similarities.
The connection to existing knowledge may not be clear here, so be intentional when you guide your pair to make the inference.
If it's still not clear to them, make the inference clear, and then encourage them to recall the specific problem and solution.

This approach is greatly assisted with command history (`history`, `history | grep`) or personal notes that can be perused.

### "give the answer"

Another important approach is "give the answer".
Or in other words, "just answer my damn question!".
Sometimes the best thing to do is give the answer and continue working.
Not every scenario needs to be a teachable moment.
It is important to be pragmatic.

"Give the answer" is very quick to use and gets the job done.
If you are short on time, this is a great choice.
Notice if you are using it too much though, you may be missing out on opportunities to enable.

### My toolbox

I consider all of the above approaches to be pairing "tools" in my toolbox as a technical consultant.
Which tool you use in any given situation is your choice; you are the world expert on knowing your exact situation.
You can start with one approach, notice it isn't working effectively, and then try another one.
If all else fails, you can always "give the answer".

Sometimes, I'll even use "find out together" when I already know the answer and I could just "give the answer" because the moment is prime for enabling.

If we have recently learnt something, I'll "drop some hints" to intentionally reinforce those newly created neural pathways.
These hints will often lead to "review the tools" in situations heavy in writing commands.

This is my toolbox.
I hope by sharing this, it can also help you with your technical consulting.

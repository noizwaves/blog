---
layout: post
title:  "Better HTML with CSS frameworks using SMACL"
tags: css semantic framework SMACL
---

# Goals

1. Style CSS Zen Garden using a style framework
1. Use the style framework to also style an application


# TL;DR

- Introduce the concept of "Semantically Mapped Anti-Corruption Layer" (or SMACL)

1. Consider CSS Frameworks as 1st class dependencies
1. Isolate framework-specific details with an anti-corruption layer
1. Use semantic classes in HTML, and map these to CSS framework classes in an ACL


# "Modern Application HTML"

- "Modern Application HTML" is the phrase I use to describe the HTML of web applications created in the presence of a modern CSS framework
  - Modern CSS Framework = Bootstrap, Materialize, et. al.
  - "You know it when you see it"
- Let's start with a concrete code example that illustrates this phenomenon
- The following code is taken from https://getbootstrap.com/docs/4.0/examples/blog/
- Look at it for a moment
- Take some time to grok it, *feel* it

```html
<div class="row mb-2">
  <div class="col-md-6">
    <div class="card flex-md-row mb-4 box-shadow h-md-250">
      <div class="card-body d-flex flex-column align-items-start">
        <strong class="d-inline-block mb-2 text-primary">World</strong>
        <h3 class="mb-0">
          <a class="text-dark" href="#">Featured post</a>
        </h3>
        <div class="mb-1 text-muted">Nov 12</div>
        <p class="card-text mb-auto">This is additional content.</p>
        <a href="#">Continue reading</a>
      </div>
      <img class="card-img-right flex-auto d-none d-md-block" src="pic.png">
    </div>
  </div>
</div>
```

- This *feels* like Bootstrap
  - It doesn't feel like a blog
  - It doesn't scream blog markup
- At most, it shouts some visual clues, but that's about it


# Frameworks are great, but they lead to problems

- The purpose of this post is not to bash Modern CSS Frameworks
  - They are great and powerful in so many ways
  - Enabled me to rapidly build good looking web applications
- I'm not artistic in the design sense, and my ability to craft CSS from scratch is weak

- With this power comes risk
- When developers use CSS frameworks, they are at risk of writing HTML with the following problems

## 1. HTML is tightly coupled to the framework (and not semantic domain)

- CSS Frameworks force certain element tree structures and class names to obtain promised styles
  - It "looks like" Bootstrap markup
  - (Does it look like "blog" markup?)
- Changing CSS framework without changing the markup is impossible
- The CSS framework is actually a direct dependency of the HTML

- The ability to switch out one dependency for another is a huge strategic advantage
- Readers might question the applicability or relevancy of needing this choice
- Consider the license change of React of 2017

## 2. Changes in style requirements result in changes in markup, violating SRP

- The HTML now has 2 responsibilities, those are:
  1. WHAT the content is, and
  1. HOW it looks
- The SRP guides us to attribute one responsibility to one thing
- Ideally, the markup is responsible for the "what" and the stylesheet the "how"
- This violation means a style centric change will likely result in changes to the stylesheet (1) and the markup (2)
- Conversely, this also means a change to the markup will likely have style side effects

## 3. Semantic class naming is discouraged (`product-preview` vs `card card-sm card-light card-highlighted`)

- The modus operandi for applying styling from a CSS framework is to add more classes to the `class` attribute value
- This pollutes the markup
  - Any effort put into semantic class names is easily drowned out by long framework class combinations

- In the absence of appropriate existing HTMl element types (like `footer`, `article`, or `time`)
- The `class` attribute is our tool for specialising general elements to suit our domain
- In this way, `class` should describe WHAT the element represents (semantic, from the domain or ontology), not HOW it looks (stylistic)


# A solution, in a few steps

# First, fix SRP in the markup

- replace stylistic classes with semantic classes
- naming things is hard, so we have to leverage our understanding of the domain


```html
<div class="previews">
  <div class="preview">
    <p class="category">World</p>
    <a class="title" href="#">Featured post</a>
    <p class="posted-on">Nov 12</p>
    <p class="body-sample">This is additional content.</p>
    <a class="full-link" href="#">Continue reading</a>
    <img class="picture" src="pic.png">
  </div>
</div>
```

- some nesting has been removed
- class names now reflect "what" and not "how"

- Let's examine some of these transformations more closely

### "previews"

```html
<div class="row mb-2">...</div>
```

- because this markup appears to describe a preview (or sample) of a full blog post
  - the "continue reading" link is a clue.
- this really describes the set of blog post previews
- let's call it `previews`

```html
<div class="previews">...</div>
```

### "preview"

```html
<div class="col-md-6">
  <div class="card flex-md-row mb-4 box-shadow h-md-250">
    ...
  </div>
</div>
```

- this seems to describe a single blog post preview (size information, shadows, etc)
- let's call it "preview"

```html
<div class "preview">
  ...
</div>
```

## But wait, we lost all the style rules!
- this is a new problem
  - the HTML is now unstyled!
- lets fix it!

# Second, map semantic classes to style classes

- quite literally, the HTML is unstyled because our stylesheet contains no rules matching `previews`
- when we removed `row` and `mb-2` from the HTML we lost the corresponding style rules

- consider the HTML change that resulted `row mb-2` becoming `previews`
- if this were a code transformation, this would be akin to "extracting" `row mb-2` "as a variable named" `previews`
  - this is quite a powerful concept
  - we've "named" the set of classes that correctly define the style of `previews`

- Ideally, somewhere in the HTML or CSS files, `previews` could be simply defined by `row mb-2`
  - But CSS and HTML lack the ability to alias or perform this kind of substitution
  - We need more expressive language and better tooling

- Imagine a world where CSS had an alias operator
  - The final css might look something like this

```css
.previews {
  @alias 'row mb-2';
}
```

- But we live in reality, and `@alias` does not exist because it was just made up by me
- Luckily, in this reality, we have CSS Preprocessors like Sass
- Sass doesn't have alias, but it does have an `@extend` directive

```sass
.previews {
  @extends .row;
  @extends .mb-2;
}
```

- Now the stylesheet is appropriately ascribing styles onto our semantic HTML
  - `previews` is styled as `row` and `mb-2`
- Aha, an anti-corruption layer!
  - Why is this good?
  - A well understood software pattern for writing clean code that has dependencies
  - For us, the Sass code protects the HTML from upstream changes in the CSS framework


# The result

- We started with HTML that illustrated 3 problems common in Modern Application HTML
- Through the introduction of a "Semantically Mapped Anti-Corruption Layer" (or SMACL), we've
  - De-coupled our HTML from framework
  - Moved responsibility of style out of the HTML and into the stylesheet
    - Now the HTML and stylesheet each have one and only one responsibility
    - Changes in CSS framework, CSS framework version, or style requirements are unlikely to affect the HTML
    - The semantic HTML changes with the domain
  - The semantic class names make the HTML look like it's problem domain


# What comes next?

1. An example of a SCSS framework
1. Call to action?
1. Next steps for readers?
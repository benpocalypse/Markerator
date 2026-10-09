# Markerator CSS File Format

Markerator does not yet make use of Classless CSS. This is a concept that I am only just learning about, as I'm not a Web Developer by trade. I'm old and cranky, and only remember how CSS used to work from nearly a decade ago. I will eventually evolve this expected format to comply with modern standards, but for now, this is what Markerator expects.

> [!NOTE]
  > This documentation uses the provided Blue.css as it's base. Make color choice adjustments based on your custom theme.


### Light/Dark Toggle
Markerator uses pure HTML and CSS to allow a user to select which theme variant they'd like to use. It's not persisted, so a user will have to choose their preferred theme on each page. For now, Light is the default.

The custom CSS file must include `.color-scheme-wrapper`, a `@media (prefers-color-scheme: dark)`, a `#mode_dark:checked ~ .color-scheme-wrapper`, and a `#mode_light:checked ~ .color-scheme-wrapper` section to enable the use of a light/dark mode function.

``` CSS
/* ---- Default (light) ---- */
.color-scheme-wrapper {
    --bg: #eef4fb;
    --bg-nav: #eef4fb;
    --bg-dropdown: #ffffff;
    --bg-dropdown-hover: #d9e6f5;
    --text: #2a3542;
    --text-heading: #143a63;
    --text-footer: #4a5a6e;
    --text-disabled: #8fa3ba;
    --accent: #1b4f8a;
    --accent-hover: #0a2a4d;
    --accent-contrast: #ffffff;
    --border: #b8cce4;
    --border-subtle: #dbe7f4;
    --shadow: rgba(27, 79, 138, 0.25);

    min-height: 100vh;
    background-color: var(--bg);
    color: var(--text);
}

/* ---- System prefers dark: default becomes dark ---- */
@media (prefers-color-scheme: dark) {
    .color-scheme-wrapper {
        --bg: #0f1724;
        --bg-nav: #0f1724;
        --bg-dropdown: #131c2c;
        --bg-dropdown-hover: #24314a;
        --text: #c8d4e4;
        --text-heading: #e6eef9;
        --text-footer: #8d9bb0;
        --text-disabled: #4b566b;
        --accent: #8ec5ff;
        --accent-hover: #ffffff;
        --accent-contrast: #0f1724;
        --border: #2a3548;
        --border-subtle: #1a2438;
        --shadow: rgba(0, 0, 0, 0.7);
    }
}

/* ---- User explicitly picked dark ---- */
#mode_dark:checked ~ .color-scheme-wrapper {
    --bg: #0f1724;
    --bg-nav: #0f1724;
    --bg-dropdown: #131c2c;
    --bg-dropdown-hover: #24314a;
    --text: #c8d4e4;
    --text-heading: #e6eef9;
    --text-footer: #8d9bb0;
    --text-disabled: #4b566b;
    --accent: #8ec5ff;
    --accent-hover: #ffffff;
    --accent-contrast: #0f1724;
    --border: #2a3548;
    --border-subtle: #1a2438;
    --shadow: rgba(0, 0, 0, 0.7);
}

/* ---- User explicitly picked light, overriding a dark system preference ---- */
#mode_light:checked ~ .color-scheme-wrapper {
    --bg: #eef4fb;
    --bg-nav: #eef4fb;
    --bg-dropdown: #ffffff;
    --bg-dropdown-hover: #d9e6f5;
    --text: #2a3542;
    --text-heading: #143a63;
    --text-footer: #4a5a6e;
    --text-disabled: #8fa3ba;
    --accent: #1b4f8a;
    --accent-hover: #0a2a4d;
    --accent-contrast: #ffffff;
    --border: #b8cce4;
    --border-subtle: #dbe7f4;
    --shadow: rgba(27, 79, 138, 0.25);
}
```

That will define the styling of the Light/Dark mode, but an actual toggle behavior will need to be included as well. The following shows the minimum used for the Blue.css theme.

```CSS
.mode-input { display: none; }

.mode-toggle {
    float: right;
    margin-right: 16px;
    margin-top: 12px;
    display: flex;
    gap: 4px;
}

.mode-label {
    cursor: pointer;
    padding: 6px 12px;
    color: var(--accent);
    border: 1px solid var(--border);
    border-radius: 4px;
    font-size: 14px;
    user-select: none;
}

.mode-label:hover {
    color: var(--accent-hover);
    border-color: var(--accent);
}

/* Highlight the active mode's label. */
#mode_light:checked ~ .color-scheme-wrapper label[for="mode_light"],
#mode_dark:checked ~ .color-scheme-wrapper label[for="mode_dark"] {
    background-color: var(--accent);
    color: var(--accent-contrast);
    border-color: var(--accent);
}
```

### Required Structural Sections
The following sections are what Markerator expects to be defined in order to style the generated site:

 - `.navigation-title`
 - `.navigation-title a`
 - `.navigation`
 - `.navigation a`
 - `.navigation a:hover`


```CSS
.navigation-title {
    overflow: hidden;
    position: fixed;
    top: 0;
    margin-left: 0;
    padding-left: 40%;
    width: 100%;
    align-items: center;
    background-color: var(--bg-nav);
}

.navigation-title a {
    float: left;
    color: var(--accent);
    text-align: center;
    padding: 10px 16px;
    text-decoration: none;
    font-size: 22px;
}

.navigation {
    overflow: hidden;
    position: fixed;
    top: 35px;
    margin-left: 0;
    padding-left: 40%;
    width: 100%;
    align-items: center;
    background-color: var(--bg-nav);
}

.navigation a {
    float: left;
    color: var(--accent);
    text-align: center;
    padding: 10px 16px;
    text-decoration: none;
    font-size: 16px;
}

.navigation a:hover { color: var(--accent-hover); }
```

### The Rest of the Page(s)
This section should be pretty straight forward. It defines the styling for the page Content, Pagination Section (if the site makes use of the Posts/Pages/News/Blog feature, otherwise this can be omitted) so that the styling matches the rest of the generated site. It also takes care of the Body, as well as the footer.

```CSS
.content { padding: 40px 20% 100px 20%; }
.content a { color: var(--accent); text-decoration: none; }
.content a:hover { color: var(--accent-hover); }

.pagination {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 0.75rem;
    margin: 2rem 0;
    font-size: 0.95rem;
}

.pagination-pages { display: flex; gap: 0.5rem; }

.pagination a,
.pagination-current,
.pagination-prev,
.pagination-next {
    padding: 0.4rem 0.75rem;
    border: 1px solid var(--border);
    border-radius: 4px;
    text-decoration: none;
    color: var(--accent);
}

.pagination-current {
    background: var(--accent);
    color: var(--accent-contrast);
    border-color: var(--accent);
    font-weight: bold;
}

.pagination .disabled {
    color: var(--text-disabled);
    border-color: var(--border-subtle);
    cursor: not-allowed;
}

h1, h2, h3, h4, h5, h6 { color: var(--text-heading); }

body { margin-left: 0; padding-top: 0; }

footer {
    text-align: center;
    padding: 6px;
    background-color: var(--bg-nav);
    color: var(--text-footer);
    font-size: 12px;
}
```

### Pro-tips
Rather than having to generate your site over and over and push it to either a test server, or a live site, you should use your favorite CSS playground tool of choice to verify your custom CSS file works as you expect it tool. If you're running Linux, I personally recommend checking out [Playhouse.](https://flathub.org/en/apps/re.sonny.Playhouse) It lets you past in HTML, CSS, and *shudder* Javascript and sere a live preview.

It's available as a [Flatpak](https://flatpak.org/) on [Flathub.](https://flathub.org) Get it here:

[![Playhouse](https://camo.githubusercontent.com/b77091910011cb67212607f1aaa8e0f81f350f8a4b84a8ee08f9e8f4658eae27/68747470733a2f2f646c2e666c61746875622e6f72672f6173736574732f6261646765732f666c61746875622d62616467652d656e2e737667)
](https://flathub.org/en/apps/re.sonny.Playhouse)

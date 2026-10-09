namespace Markerator;

/// <summary>
/// Global constants and shared content used across Markerator.
/// </summary>
public static class Globals
{
    /// <summary>The current Markerator version, surfaced in generated page footers.</summary>
    public static readonly string Version = "1.0.0";

    /// <summary>
    /// The built-in default stylesheet, written to <c>output/css/site.css</c> when
    /// no user-provided CSS is available. Class names must stay in sync with the
    /// markup emitted by <c>HtmlGenerator.BuildFullHtmlDocument</c> and
    /// <c>HtmlGenerator.BuildNavigation</c>.
    /// </summary>
    public static readonly string DefaultCss = @"
/* ============================================================
   Default — light/dark with radio-based mode toggle
   ============================================================ */

/* ---- Default (light) ---- */
.color-scheme-wrapper {
    --bg: #fcf7f0;
    --bg-nav: #fcf7f0;
    --bg-dropdown: #f1f1f1;
    --bg-dropdown-hover: #ddd;
    --text: #5e5e5e;
    --text-heading: #5e5e5e;
    --text-footer: #5e5e5e;
    --text-disabled: #aaa;
    --accent: #8c2c2c;
    --accent-hover: #000000;
    --accent-contrast: #fcf7f0;
    --border: #cccccc;
    --border-subtle: #eeeeee;
    --shadow: rgba(0, 0, 0, 0.2);

    min-height: 100vh;
    background-color: var(--bg);
    color: var(--text);
}

@media (prefers-color-scheme: dark) {
    .color-scheme-wrapper {
        --bg: #1a1614;
        --bg-nav: #1a1614;
        --bg-dropdown: #241f1c;
        --bg-dropdown-hover: #3a3330;
        --text: #d8d2cc;
        --text-heading: #f2ede8;
        --text-footer: #a39b94;
        --text-disabled: #6b625c;
        --accent: #f0a3a3;
        --accent-hover: #ffffff;
        --accent-contrast: #1a1614;
        --border: #3a3330;
        --border-subtle: #2a2422;
        --shadow: rgba(0, 0, 0, 0.6);
    }
}

#mode_dark:checked ~ .color-scheme-wrapper {
    --bg: #1a1614;
    --bg-nav: #1a1614;
    --bg-dropdown: #241f1c;
    --bg-dropdown-hover: #3a3330;
    --text: #d8d2cc;
    --text-heading: #f2ede8;
    --text-footer: #a39b94;
    --text-disabled: #6b625c;
    --accent: #f0a3a3;
    --accent-hover: #ffffff;
    --accent-contrast: #1a1614;
    --border: #3a3330;
    --border-subtle: #2a2422;
    --shadow: rgba(0, 0, 0, 0.6);
}

#mode_light:checked ~ .color-scheme-wrapper {
    --bg: #fcf7f0;
    --bg-nav: #fcf7f0;
    --bg-dropdown: #f1f1f1;
    --bg-dropdown-hover: #ddd;
    --text: #5e5e5e;
    --text-heading: #5e5e5e;
    --text-footer: #5e5e5e;
    --text-disabled: #aaa;
    --accent: #8c2c2c;
    --accent-hover: #000000;
    --accent-contrast: #fcf7f0;
    --border: #cccccc;
    --border-subtle: #eeeeee;
    --shadow: rgba(0, 0, 0, 0.2);
}

/* ============================================================
   Tables
   ============================================================ */

.table-wrapper {
    overflow-x: auto;
    margin: 1.5em 0;
    max-width: 100%;
}

.table-wrapper table {
    width: 100%;
    border-collapse: collapse;
}

.table-wrapper th,
.table-wrapper td {
    padding: 10px 14px;
    text-align: left;
    vertical-align: top;
    border-bottom: 1px solid var(--border-subtle);
    color: var(--text);
}

.table-wrapper th {
    color: var(--text-heading);
    font-weight: bold;
    border-bottom: 2px solid var(--border);
}

.table-wrapper th img,
.table-wrapper td img {
    max-width: 100%;
    height: auto;
    display: inline-block;
    vertical-align: middle;
}

/* ---- Mobile: stack the table into cards ---- */
@media (max-width: 700px) {
    .table-wrapper table,
    .table-wrapper tbody,
    .table-wrapper tr,
    .table-wrapper td {
        display: block;
        width: 100%;
    }

    /* Hide the header row and everything inside it. Using !important to
       defeat any cascade interference, and redundant display/visibility/
       height rules as a belt-and-braces measure. */
    .table-wrapper thead,
    .table-wrapper thead tr,
    .table-wrapper thead th {
        display: none !important;
        visibility: hidden !important;
        height: 0 !important;
        overflow: hidden !important;
        padding: 0 !important;
        border: 0 !important;
        margin: 0 !important;
    }

    .table-wrapper tr {
        margin-bottom: 1em;
        border: 1px solid var(--border);
        border-radius: 6px;
        padding: 8px 12px;
        background-color: var(--bg-dropdown);
        color: var(--text);
    }

    .table-wrapper td {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        gap: 1em;
        padding: 6px 0;
        border-bottom: 1px solid var(--border-subtle);
        text-align: right;
        background-color: transparent;
        color: var(--text);
    }

    .table-wrapper td:last-child {
        border-bottom: none;
    }

    .table-wrapper td::before {
        content: attr(data-label);
        font-weight: bold;
        color: var(--text-heading);
        text-align: left;
        flex: 0 0 auto;
        max-width: 40%;
    }

    .table-wrapper td:not([data-label])::before {
        content: """";
        display: none;
    }
}

/* ============================================================
   Structural rules
   ============================================================ */

.navigation-title {
    overflow: hidden;
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    height: 35px;
    padding-left: 40%;
    padding-right: 16px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    background-color: var(--bg-nav);
}

.navigation-title a {
    color: var(--accent);
    text-decoration: none;
    font-size: 22px;
}

.navigation {
    overflow: hidden;
    position: fixed;
    top: 35px;
    left: 0;
    right: 0;
    margin-left: 0;
    padding-left: 40%;
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

.navigation a:hover {
    color: var(--accent-hover);
}

.mode-input {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
}

.mode-toggle {
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

#mode_light:checked ~ .color-scheme-wrapper label[for=""mode_light""],
#mode_dark:checked ~ .color-scheme-wrapper label[for=""mode_dark""] {
    background-color: var(--accent);
    color: var(--accent-contrast);
    border-color: var(--accent);
}

.dropdownbutton {
    background-color: var(--accent);
    color: var(--accent-contrast);
    font-size: 16px;
    padding: 6px;
    padding-right: 40px;
    border: none;
}

.dropdown {
    position: relative;
    display: inline-block;
    float: right;
}

.dropdown-content {
    display: none;
    position: absolute;
    background-color: var(--bg-dropdown);
    min-width: 160px;
    box-shadow: 0px 8px 16px 0px var(--shadow);
    z-index: 1;
}

.dropdown-content a {
    color: var(--accent);
    padding: 12px 16px;
    text-decoration: none;
    display: block;
}

.dropdown-content a:hover {
    background-color: var(--bg-dropdown-hover);
}

.dropdown:hover .dropdown-content {
    display: block;
}

.dropdown:hover .dropbtn {
    background-color: #3e8e41;
}

.content {
    padding-left: 20%;
    padding-right: 20%;
    padding-top: 40px;
    padding-bottom: 100px;
}

.content a {
    color: var(--accent);
    text-align: left;
    text-decoration: none;
}

.content a:hover {
    color: var(--accent-hover);
}

.pagination {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 0.75rem;
    margin: 2rem 0;
    font-size: 0.95rem;
}

.pagination-pages {
    display: flex;
    gap: 0.5rem;
}

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

h1, h2, h3, h4, h5, h6 {
    color: var(--text-heading);
}

body {
    margin-left: 0;
    padding-top: 0;
}

footer {
    text-align: center;
    padding: 6px;
    background-color: var(--bg-nav);
    color: var(--text-footer);
    font-size: 12px;
}";
}

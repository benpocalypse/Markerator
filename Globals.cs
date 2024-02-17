using System;

namespace com.github.benpocalypse.markerator;

public partial class Markerator
{
    public readonly static string DefaultCss = @"
.navigation-title {
    overflow: hidden;
    position: fixed;
    top: 0px;
    margin-left: 0;
    padding-left: 40%;
    width: 100%;
    align-items: center;
    background-color: #fcf7f0;
}

.navigation-title a {
    float: left;
    color: #8c2c2c;
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
    background-color: #fcf7f0;
}

.navigation a {
    float: left;
    color: #8c2c2c;
    text-align: center;
    padding: 10px 16px;
    text-decoration: none;
    font-size: 16px;
}

.navigation a:hover {
    color: black;
}

table, th, td {
  border: 0px solid black;
  border-collapse: collapse;
}
th, td {
  padding-top: 10px;
  padding-bottom: 10px;
  padding-left: 0px;
  padding-right: 20px;
}

.dropdownbutton {
    background-color: #333;
    color: #f2f2f2;
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
    background-color: #f1f1f1;
    min-width: 160px;
    box-shadow: 0px 8px 16px 0px rgba(0,0,0,0.2);
    z-index: 1;
}

.dropdown-content a {
    color: black;
    padding: 12px 16px;
    text-decoration: none;
    display: block;
}

.dropdown-content a:hover {
    background-color: #ddd;
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
    color: #8c2c2c;
    text-align: left;
    text-decoration: none;
}

.content a:hover {
    color: black;
}

head {
    margin-left: 0;
}

h1 {
    color: #5e5e5e;
}

h2 {
    color: #5e5e5e;
}

h3 {
    color: #5e5e5e;
}

h4 {
    color: #5e5e5e;
}

h5 {
    color: #5e5e5e;
}

h6 {
    color: #5e5e5e;
}

body {
    background-color: #fcf7f0;
    color: #5e5e5e;
    margin-left: 0;
    padding-top: 0;
}

footer {
    text-align: center;
    padding: 6px;
    background-color: #fcf7f0;
    color: #5e5e5e;
    font-size: 12px;
}";
}

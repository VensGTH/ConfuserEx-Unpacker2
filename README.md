# ConfuserEx-Dynamic-Unpacker
A dynamic confuserex unpacker that relies on invoke for most things

# About

This is a fork (original can be found here: https://github.com/uvbs/ConfuserEx-Unpacker)
As the project has been stopped in 2017 I created a fork to fix some troubles I faced with.
Also updated the code to `dnlib` version `3.x` (https://github.com/0xd4d/dnlib)

# Fixes

Currently fixed string decoding in **Nested** types. The original code ignored them. 

# Usage

Original usage from the author, I'll change if there will be any changes:

```
when using this you there are 2 compulsary commands
the path and either -d or -s for static or dynamic 
then you can use -vv for string debug info and control flow info 
it will be in a different colour so you know whats verbose 
for strings it will give you method name string value and param
control flow it will tell you the case order for the method and for conditionals where it leads to if true or false
```



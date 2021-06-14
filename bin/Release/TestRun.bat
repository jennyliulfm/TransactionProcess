@ECHO OFF

ECHO This file is an instruction about how to run the application.
ECHO you could run like the following instruction:

ECHO TransactionProcessor.exe D:\Jennifer\coding\CodingPractice\test.csv
ECHO Note: for your cvs file, it supports relative path and full path.

ECHO 1. For full path, its format is: TransactionProcessor.exe D:\Jennifer\coding\CodingPractice\test.csv
ECHO 2. For relative path, its format is: TransactionProcessor.exe test.csv
ECHO Note: you could modify the last line in this file to run the data

REM: for input file under current directory
REM TransactionProcessor.exe test.csv

REM For input file in other location
REM TransactionProcessor.exe D:\Jennifer\coding\CodingPractice\test.csv

ECHO

PAUSE

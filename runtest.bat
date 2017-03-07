@echo off

rem
rem Usage sample:
rem
rem runtest cyk Tests\00-wpsample [--verbose]
rem or
rem runtest cj2 Tests\00-wpsample [--verbose]
rem

echo %1 %2-grammar.txt %2-input.txt %3
bin\Debug\cfl %1 %2-grammar.txt %3 < %2-input.txt

@echo off

rem
rem Usage sample:
rem
rem runtests cyk [--verbose]
rem or
rem runtests cj2 [--verbose]
rem

echo %1 00-wpsample-grammar.txt 00-wpsample-input.txt %2
rem type Tests\00-wpsample-grammar.txt
bin\Debug\cfl %1 Tests\00-wpsample-grammar.txt %2 < Tests\00-wpsample-input.txt

echo %1 01-basic-grammar.txt 01-basic-input.txt %2
rem type Tests\01-basic-grammar.txt
bin\Debug\cfl %1 Tests\01-basic-grammar.txt %2 < Tests\01-basic-input.txt

echo %1 02-basic-grammar.txt 02-basic-input.txt %2
rem type Tests\02-basic-grammar.txt
bin\Debug\cfl %1 Tests\02-basic-grammar.txt %2 < Tests\02-basic-input.txt

echo %1 03-basic-grammar.txt 03-basic-input.txt %2
rem type Tests\03-basic-grammar.txt
bin\Debug\cfl %1 Tests\03-basic-grammar.txt %2 < Tests\03-basic-input.txt

echo %1 04-basic-grammar.txt 04-basic-input.txt %2
rem type Tests\04-basic-grammar.txt
bin\Debug\cfl %1 Tests\04-basic-grammar.txt %2 < Tests\04-basic-input.txt

echo %1 05-basic-grammar.txt 05-basic-input.txt %2
rem type Tests\05-basic-grammar.txt
bin\Debug\cfl %1 Tests\05-basic-grammar.txt %2 < Tests\05-basic-input.txt

echo %1 06-basic-grammar.txt 06-basic-input.txt %2
rem type Tests\06-basic-grammar.txt
bin\Debug\cfl %1 Tests\06-basic-grammar.txt %2 < Tests\06-basic-input.txt

echo %1 07-basic-grammar.txt 07-basic-input.txt %2
rem type Tests\07-basic-grammar.txt
bin\Debug\cfl %1 Tests\07-basic-grammar.txt %2 < Tests\07-basic-input.txt

echo %1 08-atis-grammar.txt 08-atis-input.txt %2
rem type Tests\08-atis-grammar.txt
bin\Debug\cfl %1 Tests\08-atis-grammar.txt %2 < Tests\08-atis-input.txt

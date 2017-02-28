@echo off

echo %1 01-basic-grammar.txt 01-basic-input.txt %2
type Tests\01-basic-grammar.txt
bin\Debug\cfl %1 Tests\01-basic-grammar.txt %2 < Tests\01-basic-input.txt

echo %1 02-basic-grammar.txt 02-basic-input.txt %2
type Tests\02-basic-grammar.txt
bin\Debug\cfl %1 Tests\02-basic-grammar.txt %2 < Tests\02-basic-input.txt

echo %1 03-basic-grammar.txt 03-basic-input.txt %2
type Tests\03-basic-grammar.txt
bin\Debug\cfl %1 Tests\03-basic-grammar.txt %2 < Tests\03-basic-input.txt

echo %1 04-basic-grammar.txt 04-basic-input.txt %2
type Tests\04-basic-grammar.txt
bin\Debug\cfl %1 Tests\04-basic-grammar.txt %2 < Tests\04-basic-input.txt

echo %1 04-basic-grammar.txt 04-basic-input-big.txt %2
type Tests\04-basic-grammar.txt
bin\Debug\cfl %1 Tests\04-basic-grammar.txt %2 < Tests\04-basic-input-big.txt

echo %1 05-basic-grammar.txt 05-basic-input.txt %2
type Tests\05-basic-grammar.txt
bin\Debug\cfl %1 Tests\05-basic-grammar.txt %2 < Tests\05-basic-input.txt

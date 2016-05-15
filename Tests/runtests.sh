#!/bin/sh
mono \
  ./testrunner/xunit.runner.console.*/tools/xunit.console.exe \
  Tests/SharpIrcBotTests/bin/Release/SharpIrcBotTests.dll \
  Tests/RegexTests/bin/Release/RegexTests.dll \
  Tests/LinkInfoTests/bin/Release/LinkInfoTests.dll \
  -nocolor \
  -noappdomain \
  -parallel none
exit $?

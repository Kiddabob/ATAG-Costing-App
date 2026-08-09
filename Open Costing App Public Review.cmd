@echo off
setlocal
set "REVIEW_EXE=%~dp0output\Costing-App-Public-Review\Costing.App.PublicReview.exe"

if not exist "%REVIEW_EXE%" (
    echo The public-review app has not been built yet.
    echo Expected: %REVIEW_EXE%
    pause
    exit /b 1
)

start "" "%REVIEW_EXE%"

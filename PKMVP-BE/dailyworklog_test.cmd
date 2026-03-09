@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM ============================================================
REM PKMVP DailyWorklog API Full Test (final)
REM - 403 expected cases handled
REM - submit uses empty body to avoid IIS 411 Length Required
REM ============================================================

set "BASE=https://localhost:44380"
set "LOGIN_URL=%BASE%/api/auth/login"
set "ME_URL=%BASE%/api/auth/me"
set "WORKLOGS_URL=%BASE%/api/worklogs"

set "FROM_DATE=2026-03-01"
set "TO_DATE=2026-03-31"

set "TMP_DIR=%TEMP%\pkmvp_api_test"
if not exist "%TMP_DIR%" mkdir "%TMP_DIR%"

echo.
echo ==================================================
echo 0) LOGIN
echo ==================================================

call :LOGIN admin   admin123   TOKEN_ADMIN || goto :FAIL
call :LOGIN manager manager123 TOKEN_MGR   || goto :FAIL
call :LOGIN user    user123    TOKEN_IT    || goto :FAIL
call :LOGIN user3   user123    TOKEN_HR    || goto :FAIL

echo.
echo [TOKENS]
echo TOKEN_ADMIN=!TOKEN_ADMIN!
echo TOKEN_MGR=!TOKEN_MGR!
echo TOKEN_IT=!TOKEN_IT!
echo TOKEN_HR=!TOKEN_HR!

echo.
echo ==================================================
echo 1) TOKEN CHECK (/api/auth/me)
echo ==================================================

call :ME "!TOKEN_ADMIN!" || goto :FAIL
call :ME "!TOKEN_MGR!"   || goto :FAIL
call :ME "!TOKEN_IT!"    || goto :FAIL
call :ME "!TOKEN_HR!"    || goto :FAIL

echo.
echo ==================================================
echo 2) CREATE WORKLOGS
echo ==================================================

call :CREATE_WORKLOG "!TOKEN_IT!" "2026-03-05" "IT approve flow" IT_WORKLOG_ID || goto :FAIL
call :CREATE_WORKLOG "!TOKEN_HR!" "2026-03-05" "HR admin flow" HR_WORKLOG_ID || goto :FAIL

echo.
echo [WORKLOG IDS]
echo IT_WORKLOG_ID=!IT_WORKLOG_ID!
echo HR_WORKLOG_ID=!HR_WORKLOG_ID!

echo.
echo ==================================================
echo 3) ADD ITEM
echo ==================================================

call :ADD_ITEM "!TOKEN_IT!" "!IT_WORKLOG_ID!" 1 "IT Task" "IT work item" 30 50 || goto :FAIL
call :ADD_ITEM "!TOKEN_HR!" "!HR_WORKLOG_ID!" 1 "HR Task" "HR work item" 20 40 || goto :FAIL

echo.
echo ==================================================
echo 4) LIST TEST
echo ==================================================

echo.
echo ---- IT user: scope=mine
call :LIST "!TOKEN_IT!" "mine"

echo.
echo ---- IT manager: scope=team (IT team only expected)
call :LIST "!TOKEN_MGR!" "team"

echo.
echo ---- ADMIN: scope=all
call :LIST "!TOKEN_ADMIN!" "all"

echo.
echo ---- HR user: scope=mine
call :LIST "!TOKEN_HR!" "mine"

echo.
echo ---- HR user: scope=team (403 expected)
call :LIST_EXPECT_FAIL "!TOKEN_HR!" "team"

echo.
echo ==================================================
echo 5) SUBMIT BOTH
echo ==================================================

call :SUBMIT "!TOKEN_IT!" "!IT_WORKLOG_ID!" || goto :FAIL
call :SUBMIT "!TOKEN_HR!" "!HR_WORKLOG_ID!" || goto :FAIL

echo.
echo ==================================================
echo 6) CROSS-TEAM BLOCK TEST
echo ==================================================

echo.
echo ---- HR user tries to approve IT worklog (403 expected)
call :APPROVE_EXPECT_FAIL "!TOKEN_HR!" "!IT_WORKLOG_ID!" 1 "not my team"

echo.
echo ---- manager tries to reject HR worklog (403 expected)
call :REJECT_EXPECT_FAIL "!TOKEN_MGR!" "!HR_WORKLOG_ID!" "cross-team reject"

echo.
echo ==================================================
echo 7) SUCCESS PATH TEST
echo ==================================================

echo.
echo ---- manager approves IT worklog (200 expected)
call :APPROVE "!TOKEN_MGR!" "!IT_WORKLOG_ID!" 5 "approved by manager" || goto :FAIL

echo.
echo ---- admin approves HR worklog (200 expected)
call :APPROVE "!TOKEN_ADMIN!" "!HR_WORKLOG_ID!" 5 "approved by admin" || goto :FAIL

echo.
echo ==================================================
echo DONE
echo ==================================================
goto :EOF


:LOGIN
REM %1=loginId %2=password %3=outVarName
set "LID=%~1"
set "LPW=%~2"
set "OUTVAR=%~3"
set "OUTFILE=%TMP_DIR%\login_%LID%.json"
set "TOKEN="
set "HTTP="

echo.
echo [LOGIN] %LID%

for /f "delims=" %%S in ('
  curl -k -s -o "%OUTFILE%" -w "%%{http_code}" -X POST "%LOGIN_URL%" ^
    -H "Content-Type: application/json" ^
    -d "{\"loginId\":\"%LID%\",\"password\":\"%LPW%\"}"
') do set "HTTP=%%S"

echo HTTP=%HTTP%
type "%OUTFILE%"
echo.

if not "%HTTP%"=="200" (
  echo [ERROR] login failed for %LID%
  exit /b 1
)

for /f "usebackq delims=" %%T in (`
  powershell -NoProfile -Command ^
    "$j = Get-Content '%OUTFILE%' -Raw | ConvertFrom-Json; [Console]::Write($j.accessToken)"
`) do set "TOKEN=%%T"

if "%TOKEN%"=="" (
  echo [ERROR] token parse failed for %LID%
  exit /b 1
)

set "%OUTVAR%=%TOKEN%"
exit /b 0


:ME
REM %1=token
set "TKN=%~1"
set "OUTFILE=%TMP_DIR%\me_%RANDOM%.json"
set "HTTP="

echo [ME]

for /f "delims=" %%S in ('
  curl -k -s -o "%OUTFILE%" -w "%%{http_code}" -X GET "%ME_URL%" ^
    -H "Authorization: Bearer %TKN%"
') do set "HTTP=%%S"

echo HTTP=%HTTP%
type "%OUTFILE%"
echo.

if not "%HTTP%"=="200" (
  echo [ERROR] /me failed
  exit /b 1
)

exit /b 0


:CREATE_WORKLOG
REM %1=token %2=workDate %3=summary %4=outVarName
set "TKN=%~1"
set "WDT=%~2"
set "SUM=%~3"
set "OUTVAR=%~4"
set "OUTFILE=%TMP_DIR%\create_%RANDOM%.json"
set "HTTP="
set "NEWID="

echo.
echo [CREATE_WORKLOG] %WDT%

for /f "delims=" %%S in ('
  curl -k -s -o "%OUTFILE%" -w "%%{http_code}" -X POST "%WORKLOGS_URL%" ^
    -H "Authorization: Bearer %TKN%" ^
    -H "Content-Type: application/json" ^
    -d "{\"workDate\":\"%WDT%\",\"summary\":\"%SUM%\"}"
') do set "HTTP=%%S"

echo HTTP=%HTTP%
type "%OUTFILE%"
echo.

if not "%HTTP%"=="200" (
  echo [ERROR] create worklog failed
  exit /b 1
)

for /f "usebackq delims=" %%I in (`
  powershell -NoProfile -Command ^
    "$j = Get-Content '%OUTFILE%' -Raw | ConvertFrom-Json; " ^
    "if($j.worklogId){[Console]::Write($j.worklogId)} elseif($j.WorklogId){[Console]::Write($j.WorklogId)} elseif($j.id){[Console]::Write($j.id)}"
`) do set "NEWID=%%I"

if "%NEWID%"=="" (
  echo [ERROR] worklogId parse failed
  exit /b 1
)

set "%OUTVAR%=%NEWID%"
exit /b 0


:ADD_ITEM
REM %1=token %2=worklogId %3=seq %4=title %5=desc %6=spentMinutes %7=progressPct
set "TKN=%~1"
set "WID=%~2"
set "SEQ=%~3"
set "TTL=%~4"
set "DSC=%~5"
set "SPM=%~6"
set "PCT=%~7"
set "OUTFILE=%TMP_DIR%\item_%RANDOM%.json"
set "HTTP="

echo.
echo [ADD_ITEM] worklogId=%WID%

for /f "delims=" %%S in ('
  curl -k -s -o "%OUTFILE%" -w "%%{http_code}" -X POST "%WORKLOGS_URL%/%WID%/items" ^
    -H "Authorization: Bearer %TKN%" ^
    -H "Content-Type: application/json" ^
    -d "{\"seq\":%SEQ%,\"title\":\"%TTL%\",\"description\":\"%DSC%\",\"spentMinutes\":%SPM%,\"progressPct\":%PCT%}"
') do set "HTTP=%%S"

echo HTTP=%HTTP%
type "%OUTFILE%"
echo.

if not "%HTTP%"=="200" (
  echo [ERROR] add item failed
  exit /b 1
)

exit /b 0


:LIST
REM %1=token %2=scope
set "TKN=%~1"
set "SCOPE=%~2"
set "OUTFILE=%TMP_DIR%\list_%SCOPE%_%RANDOM%.json"
set "HTTP="

for /f "delims=" %%S in ('
  curl -k -s -o "%OUTFILE%" -w "%%{http_code}" -X GET "%WORKLOGS_URL%?fromDate=%FROM_DATE%&toDate=%TO_DATE%&scope=%SCOPE%" ^
    -H "Authorization: Bearer %TKN%"
') do set "HTTP=%%S"

echo HTTP=%HTTP%
type "%OUTFILE%"
echo.
exit /b 0


:LIST_EXPECT_FAIL
REM %1=token %2=scope
set "TKN=%~1"
set "SCOPE=%~2"
set "OUTFILE=%TMP_DIR%\list_fail_%SCOPE%_%RANDOM%.json"
set "HTTP="

for /f "delims=" %%S in ('
  curl -k -s -o "%OUTFILE%" -w "%%{http_code}" -X GET "%WORKLOGS_URL%?fromDate=%FROM_DATE%&toDate=%TO_DATE%&scope=%SCOPE%" ^
    -H "Authorization: Bearer %TKN%"
') do set "HTTP=%%S"

echo HTTP=%HTTP%
type "%OUTFILE%"
echo.

if "%HTTP%"=="403" (
  echo [OK] expected forbidden
) else (
  echo [WARN] expected 403, but got %HTTP%
)

exit /b 0


:SUBMIT
REM %1=token %2=worklogId
set "TKN=%~1"
set "WID=%~2"
set "OUTFILE=%TMP_DIR%\submit_%RANDOM%.json"
set "HTTP="

echo.
echo [SUBMIT] worklogId=%WID%

for /f "delims=" %%S in ('
  curl -k -s -o "%OUTFILE%" -w "%%{http_code}" -X POST "%WORKLOGS_URL%/%WID%/submit" ^
    -H "Authorization: Bearer %TKN%" ^
    -H "Content-Type: application/json" ^
    --data ""
') do set "HTTP=%%S"

echo HTTP=%HTTP%
type "%OUTFILE%"
echo.

if not "%HTTP%"=="200" (
  echo [ERROR] submit failed
  exit /b 1
)

exit /b 0


:APPROVE
REM %1=token %2=worklogId %3=score %4=commentTxt
set "TKN=%~1"
set "WID=%~2"
set "SCORE=%~3"
set "CMT=%~4"
set "OUTFILE=%TMP_DIR%\approve_%RANDOM%.json"
set "HTTP="

echo.
echo [APPROVE] worklogId=%WID%

for /f "delims=" %%S in ('
  curl -k -s -o "%OUTFILE%" -w "%%{http_code}" -X POST "%WORKLOGS_URL%/%WID%/approve" ^
    -H "Authorization: Bearer %TKN%" ^
    -H "Content-Type: application/json" ^
    -d "{\"score\":%SCORE%,\"commentTxt\":\"%CMT%\"}"
') do set "HTTP=%%S"

echo HTTP=%HTTP%
type "%OUTFILE%"
echo.

if not "%HTTP%"=="200" (
  echo [ERROR] approve failed
  exit /b 1
)

exit /b 0


:APPROVE_EXPECT_FAIL
REM %1=token %2=worklogId %3=score %4=commentTxt
set "TKN=%~1"
set "WID=%~2"
set "SCORE=%~3"
set "CMT=%~4"
set "OUTFILE=%TMP_DIR%\approve_fail_%RANDOM%.json"
set "HTTP="

echo.
echo [APPROVE_EXPECT_FAIL] worklogId=%WID%

for /f "delims=" %%S in ('
  curl -k -s -o "%OUTFILE%" -w "%%{http_code}" -X POST "%WORKLOGS_URL%/%WID%/approve" ^
    -H "Authorization: Bearer %TKN%" ^
    -H "Content-Type: application/json" ^
    -d "{\"score\":%SCORE%,\"commentTxt\":\"%CMT%\"}"
') do set "HTTP=%%S"

echo HTTP=%HTTP%
type "%OUTFILE%"
echo.

if "%HTTP%"=="403" (
  echo [OK] expected forbidden
) else (
  echo [WARN] expected 403, but got %HTTP%
)

exit /b 0


:REJECT_EXPECT_FAIL
REM %1=token %2=worklogId %3=commentTxt
set "TKN=%~1"
set "WID=%~2"
set "CMT=%~3"
set "OUTFILE=%TMP_DIR%\reject_fail_%RANDOM%.json"
set "HTTP="

echo.
echo [REJECT_EXPECT_FAIL] worklogId=%WID%

for /f "delims=" %%S in ('
  curl -k -s -o "%OUTFILE%" -w "%%{http_code}" -X POST "%WORKLOGS_URL%/%WID%/reject" ^
    -H "Authorization: Bearer %TKN%" ^
    -H "Content-Type: application/json" ^
    -d "{\"commentTxt\":\"%CMT%\"}"
') do set "HTTP=%%S"

echo HTTP=%HTTP%
type "%OUTFILE%"
echo.

if "%HTTP%"=="403" (
  echo [OK] expected forbidden
) else (
  echo [WARN] expected 403, but got %HTTP%
)

exit /b 0


:FAIL
echo.
echo ==================================================
echo TEST FAILED
echo ==================================================
exit /b 1
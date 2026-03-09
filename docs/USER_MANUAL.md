# PKMVP 사용자 매뉴얼

작성일: 2026-03-09  
대상: `USER` 권한 사용자 (일반 구성원)

## 1. 목적
- 팀 업무를 등록/갱신하고, 댓글·멘션·알림으로 협업합니다.
- 보드에서 이슈 상태를 확인하고 가능한 범위에서 진행 상태를 변경합니다.
- 일일 업무일지(Daily Worklog)를 작성/제출합니다.

## 2. 로그인
1. 로그인 화면에서 `loginId`, `password`를 입력합니다.
2. 로그인 성공 시 기본 진입 화면은 `Board`입니다.
3. 좌측 하단에서 내 `role`, `teamId`를 확인할 수 있습니다.

## 3. 메뉴 안내
- `Board`: 프로젝트/보드/스프린트별 이슈 현황(칸반)
- `Tasks`: 내/팀 업무 목록, 진행이력, 댓글
- `Daily Worklog`: 일일 업무일지 작성/제출
- `Team Directory`: 팀/사용자 검색
- `Notifications`: 멘션/댓글 알림 확인

## 4. Board 사용법
### 4.1 조회
1. `Project`, `Board`를 선택합니다.
2. 필요 시 `Sprint Filter`, `Status Filter`를 적용합니다.
3. 각 칼럼(TODO, IN_PROGRESS, BLOCKED, DONE, CANCELED)에서 이슈를 확인합니다.

### 4.2 상태 변경(드래그 앤 드롭)
1. 이슈 카드를 드래그하여 목표 상태 칼럼에 드롭합니다.
2. 권한/워크플로우상 불가능한 전이는 자동 차단되고 경고가 표시됩니다.
3. 카드 하단의 `next:` 표시로 다음 가능 상태를 확인할 수 있습니다.

### 4.3 USER 전이 규칙
- 공통 전이 맵  
`TODO -> IN_PROGRESS/BLOCKED/CANCELED`  
`IN_PROGRESS -> BLOCKED/DONE/CANCELED`  
`BLOCKED -> IN_PROGRESS/CANCELED`  
`DONE -> IN_PROGRESS/CANCELED`  
`CANCELED -> IN_PROGRESS`
- USER 추가 제한
- `CANCELED`로 전이 불가
- `DONE -> IN_PROGRESS` 재오픈 불가

## 5. Tasks 사용법
### 5.1 목록 조회
1. `Tasks` 진입 후 scope를 선택합니다.
2. USER는 기본적으로 `mine`을 사용합니다.
3. 행을 클릭하면 Progress/Comments 상세 다이얼로그가 열립니다.

### 5.2 Progress 등록
1. `status`, `progressPct`, `spentMinutes`, `commentTxt`를 입력합니다.
2. 상태 선택 목록은 현재 상태/권한 기준으로 가능한 전이만 노출됩니다.
3. `등록` 버튼으로 반영합니다.

### 5.3 댓글 작성 및 멘션
1. 댓글 입력 후 `댓글 등록` 클릭
2. 멘션 형식
- `@123` (사용자 ID)
- `@user123` (user + 숫자 ID)
- `@loginId` (예: `@manager`)
3. 멘션 대상에게 `TASK_MENTION` 알림이 생성됩니다.
4. 멘션 외 댓글은 담당자/보고자에게 `TASK_COMMENT` 알림이 생성됩니다(작성자 제외).

## 6. Notifications 사용법
1. `Notifications` 메뉴에서 알림 목록을 확인합니다.
2. `unread only` 체크로 미읽음만 필터링합니다.
3. 개별 `읽음` 또는 `전체 읽음` 처리할 수 있습니다.
4. 좌측 메뉴 `Notifications` 옆 배지에서 미읽음 개수를 즉시 확인할 수 있습니다.

## 7. Daily Worklog 사용법
1. `Create Worklog`에서 날짜/요약 입력 후 생성
2. 생성한 항목에 `Add Item`으로 상세 작업 추가
3. `Submit`으로 제출
4. 승인/반려는 관리자(또는 팀장) 절차에 따릅니다.

## 8. Team Directory 사용법
1. 팀 선택 또는 검색어 입력
2. `검색` 클릭
3. 사용자 ID, 이름, 팀, 역할 확인

## 9. 자주 발생하는 문제
- 보드에서 카드가 안 움직임
- 현재 상태/권한에서 허용되지 않은 전이입니다. 카드 하단 `next:`를 확인하세요.
- 댓글 알림이 안 옴
- 본인이 작성한 댓글은 본인에게 알림을 보내지 않습니다.
- 멘션 문법 오류
- `@` 뒤 토큰은 영문/숫자/`_.-` 조합으로 입력하세요.

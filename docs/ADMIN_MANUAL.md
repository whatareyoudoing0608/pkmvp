# PKMVP 관리자 매뉴얼

작성일: 2026-03-09  
대상: `MANAGER`, `ADMIN`

## 1. 목적
- Jira 스타일 협업 운영(프로젝트/보드/스프린트/이슈 계획)을 관리합니다.
- 팀 업무 흐름(상태 전이, 진행률, 코멘트, 멘션 알림)을 안정적으로 운영합니다.
- 일일 업무일지 승인/반려 및 팀 리포트를 관리합니다.

## 2. 권한 요약
### 2.1 공통 (`MANAGER`, `ADMIN`)
- Board > `Manager Actions` 사용 가능
- 프로젝트/보드/스프린트 생성
- 스프린트 상태 변경(PLANNED/ACTIVE/CLOSED)
- 신규 이슈 생성 + 보드 연결
- 기존 Task를 보드/Sprint에 계획 반영
- Reports 화면 접근 가능
- Daily Worklog 승인/반려 가능

### 2.2 `ADMIN` 추가 범위
- Tasks/Worklogs 조회 시 `scope=all` 사용 가능
- 전사 단위 조회 및 점검에 활용

## 3. 운영 시작 전 체크리스트
1. DB 마이그레이션 적용 여부 확인
2. BE/FE 빌드 확인
- BE: `dotnet build C:\Scourecode\manager\PKMVP-BE_FE\PKMVP\Pkmvp.Api\Pkmvp.Api.sln`
- FE: `npm.cmd run build` (경로: `C:\Scourecode\manager\PKMVP-BE_FE\PKMVP-FE\employee-manager-fe`)
3. BE 기동 후 Swagger 확인
- `/swagger/index.html`
- 알림 unread API: `/api/notifications/unread-count`

## 4. Jira 스타일 보드 운영 절차
### 4.1 프로젝트/보드 생성
1. `Board` 화면 진입
2. `Create Project`에 `projectKey`, `name` 입력 후 생성
3. `Create Board`에 보드명/보드타입(`KANBAN` 또는 `SCRUM`) 선택 후 생성

### 4.2 스프린트 운영
1. `Create Sprint`에서 스프린트 생성
2. `Sprints` 테이블에서 상태 전환
- 준비: `PLANNED`
- 진행: `ACTIVE`
- 종료: `CLOSED`

### 4.3 이슈 계획
1. 신규 이슈는 `Create Issue In Board`로 생성
- 필수: 제목
- 선택: 설명, 타입, priority(1~5), assigneeId, sprint
2. 기존 Task는 `Plan Existing Task`로 보드/Sprint에 반영

## 5. 워크플로우 정책
### 5.1 상태 전이 맵
- `TODO -> IN_PROGRESS/BLOCKED/CANCELED`
- `IN_PROGRESS -> BLOCKED/DONE/CANCELED`
- `BLOCKED -> IN_PROGRESS/CANCELED`
- `DONE -> IN_PROGRESS/CANCELED`
- `CANCELED -> IN_PROGRESS`

### 5.2 권한별 차이
- `MANAGER/ADMIN`: 전이 맵 내 전이 허용
- `USER`: `CANCELED` 금지, `DONE -> IN_PROGRESS` 금지

### 5.3 UI 확인 포인트
- Board 카드 하단 `next:` 라인으로 허용 전이 확인
- 불가능 전이는 드래그 시 경고 토스트 표시
- Tasks 상세의 status 드롭다운도 동일 정책 적용

## 6. 댓글/멘션/알림 운영
### 6.1 멘션 규칙
- 지원 형식: `@123`, `@user123`, `@loginId`
- 멘션 알림 타입: `TASK_MENTION`
- 일반 댓글 알림 타입: `TASK_COMMENT`

### 6.2 알림 운영
- Notifications 화면에서 개별/전체 읽음 처리
- 좌측 메뉴 배지로 unread 개수 확인
- 팀 내 멘션 규칙(로그인 ID 표준) 사전 공유 권장

## 7. Daily Worklog 운영
1. 팀원이 작성/제출한 Worklog를 조회
2. 필요 시 `Approve`(점수/코멘트) 또는 `Reject`(사유 코멘트)
3. 반려 사유는 재작성 가능하도록 구체적으로 기록

## 8. Reports 운영
1. `Reports`에서 기간(오늘/7일/30일/사용자 지정) 선택
2. 팀원 필터(선택) 적용
3. Task/Worklog KPI 확인
- Task 총건수, 완료율, 평균 진행률
- Worklog 상태별 건수
4. `Print/PDF`로 공유용 리포트 생성

## 9. 장애 대응 가이드
- 보드 전이 실패 다발
- 권한(ROLE)과 워크플로우 정책, 프론트 최신 배포 여부 확인
- 알림 배지/목록 불일치
- `notifications:refresh` 이벤트 동작, `/api/notifications/unread-count` 응답 확인
- 댓글 멘션 미동작
- 입력 형식(`@loginId`, `@user123`, `@숫자ID`)과 서버 `Auth:Users` 매핑 확인

## 10. 운영 권장사항
- 운영계/개발계정 분리(테스트 계정 직접 운영 사용 금지)
- 주간 스프린트 종료 시 `CLOSED` 정리
- priority 기준(1~5)과 issue type 기준 문서화
- 팀 공통 멘션 규칙(loginId 기준) 고정

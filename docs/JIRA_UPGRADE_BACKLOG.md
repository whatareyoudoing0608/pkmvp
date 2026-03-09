# Jira Backlog Template - BE/FE/DB Upgrade

작성일: 2026-03-06
프로젝트: PKMVP

## EPIC

### [EPIC-PLATFORM-UPGRADE] BE/FE/DB 현대화 업그레이드
- Type: Epic
- Priority: Highest
- Labels: upgrade, platform, backend, frontend, database
- Goal:
  - .NET Core 3.1 기반 BE를 .NET 8으로 업그레이드
  - FE 빌드/배포 안정화
  - DB 마이그레이션 표준화(Flyway/DbUp)
  - 무중단에 가까운 배포와 롤백 체계 확보
- Definition of Done:
  - 빌드/테스트 파이프라인 녹색
  - 배포 및 롤백 런북 완료
  - DB 마이그레이션 이력 관리 자동화
  - 릴리즈 노트/운영 가이드 완료

---

## STORY / TASK

### [BE-101] .NET 8 업그레이드
- Type: Story
- Priority: Highest
- Depends on: 없음
- Description:
  - `TargetFramework`를 `net8.0`으로 전환
  - 호환되지 않는 패키지 버전 교체
  - Startup/Program 구성을 최신 방식으로 정비
- Acceptance Criteria:
  - [ ] `dotnet build` 경고 0 / 오류 0
  - [ ] 주요 API 스모크 테스트 통과(auth, tasks, worklogs)
  - [ ] 로컬/QA 환경에서 앱 기동 확인
- Sub-tasks:
  - [BE-101-1] csproj 타겟 프레임워크 변경
  - [BE-101-2] 패키지 호환성 검토 및 교체
  - [BE-101-3] 미들웨어/인증 파이프라인 회귀 점검

### [BE-102] 인증/권한 회귀 테스트 자동화
- Type: Task
- Priority: High
- Depends on: BE-101
- Description:
  - JWT 발급/검증, role/scope 인가 시나리오 자동화
- Acceptance Criteria:
  - [ ] 관리자/팀장/사용자 권한 시나리오 테스트 통과
  - [ ] 만료 토큰/변조 토큰 실패 케이스 검증

### [DB-201] 마이그레이션 프레임워크 도입
- Type: Story
- Priority: Highest
- Depends on: 없음
- Description:
  - Flyway 또는 DbUp 도입
  - 버전 기반 SQL 관리 체계 정립
- Acceptance Criteria:
  - [ ] DEV/QA에서 동일 스키마 재현 가능
  - [ ] 마이그레이션 실행 로그/이력 추적 가능
- Sub-tasks:
  - [DB-201-1] 도구 선정(Flyway vs DbUp)
  - [DB-201-2] baseline 스크립트 작성
  - [DB-201-3] CI 파이프라인 연동

### [DB-202] 데이터 마이그레이션/백필
- Type: Story
- Priority: High
- Depends on: DB-201
- Description:
  - 기존 데이터 정합성 검증 및 백필 스크립트 작성
- Acceptance Criteria:
  - [ ] 핵심 테이블 row count 검증 통과
  - [ ] 제약조건/인덱스 영향 분석 완료
  - [ ] 롤백 SQL 또는 복구 절차 문서화

### [FE-301] FE 의존성 및 빌드/배포 체계 정비
- Type: Story
- Priority: Medium
- Depends on: 없음
- Description:
  - React/Vite/TS 버전 정합성 점검
  - 환경변수/빌드 명령/실행 가이드 정리
- Acceptance Criteria:
  - [ ] `npm run build` 성공
  - [ ] 핵심 화면(로그인/업무일지/작업관리) 수동 테스트 통과
  - [ ] 실행 가이드 문서 최신화

### [OPS-401] 배포/롤백 런북 구축
- Type: Story
- Priority: Highest
- Depends on: BE-101, DB-202
- Description:
  - 단계적 배포(Blue/Green 또는 Canary) 절차와 롤백 기준 정의
- Acceptance Criteria:
  - [ ] 배포 체크리스트 승인
  - [ ] 장애 시 15분 내 복구 리허설 완료
  - [ ] 운영 담당자 인수인계 완료

### [QA-501] 통합 검증 및 성능 베이스라인
- Type: Story
- Priority: High
- Depends on: BE-101, FE-301, DB-202
- Description:
  - API 계약 테스트 + FE-BE 연동 + 성능 기준 수립
- Acceptance Criteria:
  - [ ] 주요 API 계약 테스트 통과
  - [ ] E2E 핵심 경로 통과
  - [ ] 기준 성능(응답시간/에러율) 문서화

---

## 권장 생성 순서 (Jira 등록 순)
1. EPIC-PLATFORM-UPGRADE
2. DB-201
3. BE-101
4. FE-301
5. DB-202
6. BE-102
7. QA-501
8. OPS-401

## Sprint 제안
- Sprint 1 (2주): DB-201, BE-101 착수
- Sprint 2 (2주): BE-101 완료, FE-301, DB-202
- Sprint 3 (2주): BE-102, QA-501, OPS-401

## 리스크 메모
- .NET 3.1 -> 8.0 전환 시 인증/미들웨어 동작 차이 가능
- Oracle 드라이버/SQL 호환성 이슈 가능
- DB 변경은 반드시 백업 및 롤백 리허설 후 반영

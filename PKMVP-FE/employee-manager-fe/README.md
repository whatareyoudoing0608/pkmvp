# 직원관리 FE (Vite + React + TypeScript)

이 프로젝트는 직원관리(Tasks / DailyWorklog / TeamDirectory) FE 샘플입니다.

## 실행
```bash
npm install
npm run dev
```
- 기본 접속: http://localhost:5173
- PowerShell 실행 정책으로 `npm`이 막히면 `npm.cmd install`, `npm.cmd run dev`를 사용하세요.

## API Base URL
루트에 `.env` 파일을 만들고 아래처럼 설정하세요.
```env
VITE_API_BASE_URL=https://localhost:44380
```

## BE 엔드포인트 매핑
- `src/api/endpoints.ts` 만 먼저 실제 BE 라우팅에 맞추면 됩니다.

## 빌드/프리뷰
```bash
npm run build
npm run preview
```


## Worklogs API 스펙(테스트 스크립트 기준)
- 로그인: POST /api/auth/login body {loginId,password}
- 목록: GET /api/worklogs?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD&scope=mine|team|all
- 생성: POST /api/worklogs body {workDate,summary}
- 아이템추가: POST /api/worklogs/{id}/items
- 제출: POST /api/worklogs/{id}/submit (빈 body)
- 승인: POST /api/worklogs/{id}/approve body {score,commentTxt}
- 반려: POST /api/worklogs/{id}/reject body {commentTxt}




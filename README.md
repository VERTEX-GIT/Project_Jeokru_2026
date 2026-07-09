# Project_Jukru_2026
2026년 버텍스 게임부 프로젝트 중 하나.   
     
     
---

**주의사항**
- 처음으로 유니티 열 때는 **새 유니티 파일 만들지 말고**, 이 레포지토리 받은 후 **'Add->Add project from disk'** 하기
- 유니티 버전은 **6000.4.7f1** 입니다.
- 용도에 맞는 브랜치 만들고(ex: feat/player-move) 다 만들면 main에 합칠 것임. **기능 다 만들면 Pull Request** 하세요!
- **9/30일에 이거 전시해야 합니다.**

---

## Commit Convention
커밋 컨벤션: 커밋할 때 앞에 붙이는 거 규칙  
(ex: feat: 플레이어 이동 구현)  

- feat: 새로운 기능 추가  
- fix: 버그 수정
- docs: 문서 수정
- test: 테스트 코드 수정, 리팩토리 테스트 코드 추가
- chore: 그 외 자잘한 수정에 대한 커밋
- ----------(아래는 굳이 몰라도 될 듯..?)-----------
- hotfix: 긴급한 버그 수정
- build: 빌드 관련 파일 수정, 패키지 매니저 수정  
- ci: CI 관련 설정 수정에 대한 커밋    
- style: 코드 포맷팅, 세미콜론 누락, 코드 변경이 없는 경우  
- refactor: 코드 리팩토링  

## Branch Naming
커밋처럼 브랜치도 네이밍 규칙이 있다.  
(ex: feature/login-page)  

- feat/: 기능 추가
- fix/: 버그 수정
- exp/: 실험적 기능 테스트
- (이름)/: 개인 작업 브랜치
- hotfix/: 긴급한 버그 수정
  
브랜치 이름은 반드시 **'(접두사)/'** 로 시작한다.  
또한, 브랜치 작명 시 **띄어쓰기는 '-'**, **전부 소문자**를 사용한다.  

**반드시 main 브랜치는 최종 완성본만 남기도록 한다. 기능 구현시 무조건 branch 만들도록!**  

## Pull Request(PR)
main에 연결하기 위해선 필수적으로 Pull Request를 해야한다. (그리고 내가 강제로 그렇게만 가능하게 설정해두었다!)  
다른 브랜치에서 작업해서 완성한 결과물을 최종적으로 main에 merge 해야 하는데,  
그걸 막 합쳐서 개판나지 않기 위해 존재하는 것이 PR이다.  
     
아래에 기재된 내용을 따라 PR을 하면 된다.  
   
<img width="745" height="456" alt="image" src="https://github.com/user-attachments/assets/00ab614b-1ade-4403-837d-38a672017817" />
   
1. 깃허브 리포지토리에서 자신이 작업하던 브랜치를 선택한 후, Compare & pull request를 누른다.  
2. 'Create Pull Request'를 누른다.

<img width="1391" height="1174" alt="image" src="https://github.com/user-attachments/assets/6badcaee-5c6b-4b74-b882-eb5dd96c2094" />

3. 승인해줄 때까지 기다린다! (내가 승인 안하면 main에 merge 못 함. **'Review Required', 'Merging is blocked' 뜨는 게 정상이다**)

<img width="1337" height="1001" alt="image" src="https://github.com/user-attachments/assets/6e56eaa5-439c-4502-a44b-9fe49584a207" />

4. 만약 Changes Requested라면, 피드백 한 사항을 수정한다.  
5. 수정하고 나에게 말해주면, 내가 확인하고 Approve 할 것이다!  
6. 만약 승인이 났음에도 Merge되지 않았다면 직접 merge한다. (왠만하면 승인과 동시에 내가 merge 할 것이다.) 

<img width="692" height="409" alt="image" src="https://github.com/user-attachments/assets/4572c605-f38c-4bff-aa97-033340a2c3fa" />


## Issue
버그, 새 기능 추가 및 개선 사항 제안, 해야 할 일 등을 기록한 것이다. 깃허브 Issue 탭에서 추가 가능하다.  
작업이 진행 중이면 Open, 끝나면 Close 라고 하며, 깃허브에서도 그렇게 설정해야한다.  
  
<img width="1186" height="386" alt="image" src="https://github.com/user-attachments/assets/2b1fc8d6-485d-40ae-8a5d-aba1b4c1ecb4" />

선택한 양식에 따라 작성하면 되며, 신경 써야 할 것은 제일 하단에 있는 것들이다.  
나중에도 수정해서 추가할 수 있으나, 왠만하면 처음부터 다 채우는 것이 좋다. 상황에 따라 다르겠지만.  
<img width="1172" height="922" alt="image" src="https://github.com/user-attachments/assets/64cfc16d-8839-4ad6-b879-3f5b9cbe3d6b" />
- Assignee: 이 이슈를 맡은 담당자
- Label: 이 이슈의 분류. 여러 개 설정이 가능하다. 아래에 있는 라벨만 알아도 된다.
  - bug: 버그에 관련된 이슈일 때
  - documentation: 문서(readme 등) 업데이트 같은 작업일 때
  - duplicate: 중복된 이슈 또는 PR일 때
  - enhancement: 개선사항에 대한 것일 때
  - feature: 새 기능 제안에 대한 것일 때
  - help wanted: 도움이 필요한 이슈일 때
  - question: 더 많은 정보가 필요하거나 질문이 있는 이슈일 때
  - wontfix: 문제를 수정하지 않기로(방치하기로) 할 때
- Issue Type: 이 이슈의 타입. 초반에 선택한 템플릿에 따라 미리 설정되어 있을 것이다.
- Priority: 이슈의 중요도 [최상/상/중/하]
- Effort: 어려움 정도. 굳이 할 필요 없음.
- Project: 이슈가 보여질 프로젝트(나중에 설명). 무조건 **'Game Development - Project Jukru'로 설정하기!**
- Milestone: 날짜까지 정한 목표라고 보면 될 듯. 한 개만 설정 가능.
  
## Project
깃허브에서 작업 현황과 진행도를 볼 수 있는 것. Issue와 같이 깃허브 Projects 탭에서 확인 가능하다.  
프로젝트엔 여러 컬럼들이 있으며, 그 컬럼에 생성한 이슈들이 존재한다. 컬럼은 다음과 같다:
- Backlog: 아직 시작하지 않은 이슈
- Ready: 곧 착수할 이슈
- In Progress: 진행 중인 이슈
- In Review: PR 대기 및 결과물 리뷰
- Done: 개발이 끝난 작업

프로젝트에 들어가면 Backlog, Priority board 등등이 적힌 탭이 있는데 그냥 Backlog만 봐도 된다.        
  
##
  
나머진 알아서 잘 할 수 있을 거라고 생각한다. 기초도 기억 안 난다면 아쉬운 거고...

---

//EMP 테이블 조회
SELECT *FROM EMP;

SELECT ENAME, SAL, NVL(COMM, 0) FROM EMP; --NULL 제거하고 뽑음

SELECT ENAME, SAL, NVL2(COMM, 'O', 'X') FROM EMP; -- OX 대신 한글로 가능


----------------------------------------------------------------------------------
//실습6-47
SELECT EMPNO, ENAME, JOB, SAL,
DECODE(JOB, 'MANAGER', SAL*1.1,
        'SALESMAN', SAL*1.05,
        'ANALYST', SAL,
        SAL*1.03) AS UPSAL
        FROM EMP;


----------------------------------------------------------------------------------       
//CASE문을 사용
SELECT EMPNO, ENAME, JOB, SAL,
    CASE JOB
        WHEN 'MANAGER' THEN SAL*1.1
        WHEN 'SALESMAN' THEN SAL*1.05
        WHEN 'ANALYST' THEN SAL
        ELSE SAL*1.03
    END AS UPSAL
FROM EMP;


----------------------------------------------------------------------------------
//특정조건 CASE문
SELECT EMPNO, ENAME, SAL,
        CASE
            WHEN SAL >= 3000 THEN '상위권'
            WHEN SAL > 1000 AND SAL < 3000 THEN '중위권'
            ELSE '하위권'
        END AS 급여수준
FROM EMP;


----------------------------------------------------------------------------------
//연습문제 6장 3번 HW
--1. 입사일 기준 3개월 뒤 날짜를 구해야함.(정직원)
--1-1 형식은 YYYY/MM/DD
--1-2 첫 월요일
--2. COMM(추가수당)이 없으면 NULL이 아니라 N/A로 표현하라
SELECT EMPNO,ENAME,TO_CHAR(HIREDATE,'YYYY/MM/DD')AS HIREDATE,
TO_CHAR(NEXT_DAY(ADD_MONTHS(HIREDATE,3),'월요일'),'YYYY-MM-DD')AS R_JOB,
NVL(TO_CHAR(COMM),'N/A')AS COMM
FROM EMP;


----------------------------------------------------------------------------------
//연습문제 6장 3번
--1. 입사일 기준 3개월 뒤 날짜를 구해야함.(정직원)
--1-1 형식은 YYYY/MM/DD
--1-2 첫 월요일
--2. COMM(추가수당)이 없으면 NULL이 아니라 N/A로 표현하라
SELECT EMPNO, ENAME, HIREDATE,
        TO_CHAR(NEXT_DAY(ADD_MONTHS(HIREDATE, 3), '월요일'), 'YYYY-MM-DD' AS "정직원 되는 날",
        NVL2(COMM, TO_CHAR(COMM), 'N/A')
FROM EMP;

                
----------------------------------------------------------------------------------
//연습문제 6장 4번 HW
SELECT EMPNO,ENAME,MGR,
CASE WHEN MGR IS NULL THEN 0000
WHEN SUBSTR(MGR,-LENGTH(MGR),2)='75' THEN 5555
WHEN SUBSTR(MGR,-LENGTH(MGR),2)='76' THEN 6666
WHEN SUBSTR(MGR,-LENGTH(MGR),2)='77' THEN 7777
WHEN SUBSTR(MGR,-LENGTH(MGR),2)='78' THEN 8888
ELSE MGR END AS CHG_MGR FROM EMP;

                
----------------------------------------------------------------------------------
//연습문제 6장 4번
SELECT EMPNO, ENAME, MGR,
        CASE
            WHEN MGR IS NULL THEN TO_CHAR('0000')
            WHEN SUBSTR(MGR, 1, 2) = 75 THEN TO_CHAR('5555')
            WHEN SUBSTR(MGR, 1, 2) = 76 THEN TO_CHAR('6666')
            WHEN SUBSTR(MGR, 1, 2) = 77 THEN TO_CHAR('7777')
            WHEN SUBSTR(MGR, 1, 2) = 78 THEN TO_CHAR('8888')
            ELSE TO_CHAR(MGR)
        END AS "직급 변환값"
FROM EMP;


----------------------------------------------------------------------------------
//다중행 함수
SELECT COUNT(SAL) FROM EMP;
SELECT SUM(SAL) FROM EMP;
SELECT MAX(SAL) FROM EMP;
SELECT MIN(SAL) FROM EMP;
SELECT ROUND(AVG(SAL), 2) FROM EMP; -- 둘째 자리까지


----------------------------------------------------------------------------------
//집합연산 다중행연산 묶어서 처리하기
SELECT ROUND(AVG(SAL), 2) FROM EMP  --평균이 구해진다.
WHERE DEPTNO = 30;

                
----------------------------------------------------------------------------------
//부서별 급여 평균
SELECT ROUND(AVG(SAL), 2) FROM EMP
GROUP BY DEPTNO;

                
----------------------------------------------------------------------------------
//바로 위의 것을 내림차순으로 정렬
SELECT ROUND(AVG(SAL), 2) FROM EMP GROUP BY DEPTNO
ORDER BY DEPTNO;

                
----------------------------------------------------------------------------------
//부서별 급여의 합?
SELECT DEPTNO, SUM(SAL) FROM EMP
GROUP BY DEPTNO;


----------------------------------------------------------------------------------
//급여의 합계
SELECT SUM(DISTINCT SAL),
        SUM(ALL SAL),
        SUM(SAL)
FROM EMP;

                
----------------------------------------------------------------------------------
//행의 개수를 구함
SELECT COUNT(*) FROM EMP;

                
----------------------------------------------------------------------------------
//부서 별로 몇 명인지
SELECT DEPTNO, COUNT(*) FROM EMP
GROUP BY DEPTNO
ORDER BY DEPTNO; -- 이 문장에서 정렬.

                
----------------------------------------------------------------------------------
//퀴즈 부서번호가 10번인 사원들의 최대급여를 구하라
SELECT MAX(SAL) FROM EMP
WHERE DEPTNO = 10;

                
----------------------------------------------------------------------------------
//20번 부서 출력
SELECT * FROM EMP WHERE DEPTNO = 20;


----------------------------------------------------------------------------------
//MIN, MAX를 사용할 때 어떻게 사용해야 하는지 헷갈리지 말기
-- 부서 번호가 20인 직원 중 가장 최근에 입사한 사람의 날짜
SELECT MAX(HIREDATE) FROM EMP
WHERE DEPTNO = 20;

                
----------------------------------------------------------------------------------
//부서 번호가 20인 직원 중 가장 오래 근무한 사람의 날짜
SELECT MIN(HIREDATE) FROM EMP
WHERE DEPTNO = 20;

                
----------------------------------------------------------------------------------
//HAVING절 GROUP BY의 조건
SELECT DEPTNO, JOB, ROUND(AVG(SAL),2) FROM EMP -- 단일행 사용 밑의 GROUP BY로 묶어줌
GROUP BY DEPTNO, JOB
HAVING ROUND(AVG(SAL),2) >= 2000
ORDER BY DEPTNO, JOB; -- 부서 번호 기준으로 JOB을 정렬


----------------------------------------------------------------------------------
//WHERE절이랑 HAVING절 같이 사용    //WHERE 조건이 먼저 나와야함.
SELECT DEPTNO, JOB, AVG(SAL) FROM EMP --단일행 집합을 사용해서 
WHERE SAL <= 3000   -- 여기 에러남
GROUP BY DEPTNO, JOB -- 그래서 GROUP BY로 묶어줌
HAVING AVG(SAL) >= 2000 -- 출력
ORDER BY DEPTNO, JOB; --하는데 정렬까지
--순서대로 해야함

SELECT * FROM DEPT;
SELECT * FROM SALGRADE;
SELECT * FROM BONUS;

SELECT * FROM EMP;

                
----------------------------------------------------------------------------------
//연습문제 1
SELECT DEPTNO,
    ROUND(AVG(SAL),2), MAX(SAL), MIN(SAL), COUNT(SAL)
FROM EMP
GROUP BY DEPTNO
ORDER BY DEPTNO ASC;

                
----------------------------------------------------------------------------------
//연습문제 2
SELECT JOB, COUNT(JOB) FROM EMP
GROUP BY JOB
HAVING COUNT(JOB) >= 3;

                
----------------------------------------------------------------------------------
//연습문제 3
SELECT SUBSTR(HIREDATE, 1, 2),  COUNT(*), DEPTNO
FROM EMP
GROUP BY SUBSTR(HIREDATE, 1, 2), DEPTNO
ORDER BY SUBSTR(HIREDATE, 1, 2), DEPTNO;

SELECT TO_CHAR(HIREDATE, 'YYYY'), DEPTNO, COUNT(*)
FROM EMP
GROUP BY TO_CHAR(HIREDATE, 'YYYY'), DEPTNO
ORDER BY TO_CHAR(HIREDATE, 'YYYY'), DEPTNO;


----------------------------------------------------------------------------------
//영화 테이블 퀴즈1
SELECT GENRE, COUNT(GENRE) FROM MOVIES
GROUP BY GENRE
ORDER BY COUNT(GENRE);


----------------------------------------------------------------------------------
//영화 테이블 퀴즈2
SELECT TITLE, RUNTIME
FROM MOVIES
WHERE RUNTIME >= 130
ORDER BY RUNTIME;

                
----------------------------------------------------------------------------------
//영화 테이블 퀴즈3
SELECT
    SUM(CASE
        WHEN RELEASE_DATE <= TO_DATE('2014-12-31', 'YYYY-MM-DD') THEN 1 ELSE 0
    END) AS "2014 이전",
    SUM(CASE
        WHEN RELEASE_DATE > TO_DATE('2014-12-31', 'YYYY-MM-DD') THEN 1 ELSE 0
    END) AS "2015 이후"
FROM MOVIES;

                
----------------------------------------------------------------------------------
//영화 테이블 퀴즈4
SELECT AVG(RUNTIME) FROM MOVIES;

                
----------------------------------------------------------------------------------
//영화 테이블 퀴즈5
SELECT TITLE, RUNTIME
FROM MOVIES
WHERE RUNTIME = (SELECT MIN(RUNTIME) FROM MOVIES)
   OR RUNTIME = (SELECT MAX(RUNTIME) FROM MOVIES);



----------------------------------------------------------------------------------
//실습 7-24
SELECT DEPTNO, JOB, COUNT(*), MAX(SAL), MIN(SAL), AVG(SAL)
FROM EMP
GROUP BY DEPTNO, JOB
ORDER BY DEPTNO, JOB;


----------------------------------------------------------------------------------
//ROLLUP
SELECT DEPTNO, JOB, COUNT(*), MAX(SAL), MIN(SAL), ROUND(AVG(SAL), 2)
FROM EMP
GROUP BY ROLLUP(DEPTNO, JOB);


----------------------------------------------------------------------------------
//CUBE
SELECT DEPTNO, JOB, COUNT(*), MAX(SAL), MIN(SAL), ROUND(AVG(SAL), 2)
FROM EMP
GROUP BY CUBE(DEPTNO, JOB);


----------------------------------------------------------------------------------
//7-36 PIVOT
SELECT DEPTNO, JOB, MAX(SAL)
FROM EMP
GROUP BY DEPTNO, JOB
ORDER BY DEPTNO, JOB;

--같은 건데 PIVOT으로 하면??
                
SELECT * FROM (SELECT DEPTNO, JOB, SAL FROM EMP)
            PIVOT(MAX(SAL)
                FOR DEPTNO IN (10, 20, 30))
ORDER BY JOB;


----------------------------------------------------------------------------------
//7-37 부서별 직책별 최고 급여를 2차원 표 형태로 출력
SELECT * FROM (SELECT JOB, DEPTNO, SAL FROM EMP)
        PIVOT(MAX(SAL)
            FOR JOB IN (
            'CLERK',
            'SALESMAN',
            'PRESIDENT',
            'MANAGER',
            'ANALYST'))
ORDER BY DEPTNO;


----------------------------------------------------------------------------------
//JOIN 테이블의 결합
SELECT * FROM EMP, DEPT
WHERE EMP.DEPTNO = DEPT.DEPTNO;


----------------------------------------------------------------------------------
//테이블 별칭(실습 8-3)   테이블 두 개를 합치는 것.
SELECT * FROM EMP E, DEPT D
WHERE E.DEPTNO = D.DEPTNO
ORDER BY EMPNO;


----------------------------------------------------------------------------------
//EMP와 DEPT 테이블에 존재하는 것들을
//조인으로 최소한의 정보로 새 테이블을 만들어낼 수 있다.
//최소 하나의 컬럼은 중복이 되어야 한다.
SELECT * FROM EMP;
SELECT * FROM DEPT;

SELECT E.EMPNO, E.ENAME, D.DEPTNO, D.DNAME, D.LOC
FROM EMP E, DEPT D
WHERE E.DEPTNO = D.DEPTNO;


----------------------------------------------------------------------------------
//실습 8-6
SELECT E.EMPNO, E.ENAME, E.SAL, D.DEPTNO, D.DNAME, D.LOC
FROM EMP E, DEPT D
WHERE E.DEPTNO = D.DEPTNO;


----------------------------------------------------------------------------------
//급여가 2500 이하이고 사원 번호가 7600 이하인 사원의 정보가 출력되는 코드
SELECT E.EMPNO, E.ENAME, E.SAL, D.DEPTNO, D.DNAME, D.LOC
FROM EMP E, DEPT D
WHERE E.DEPTNO = D.DEPTNO
AND E.SAL <= 2500
AND E.EMPNO <= 7600
ORDER BY E.EMPNO;


----------------------------------------------------------------------------------
//
SELECT * FROM MOVIES;
SELECT * FROM ACTORS;
SELECT * FROM MOVIE_ACTORS;

SELECT M.TITLE, A.NAME
FROM MOVIES M , ACTORS A, MOVIE_ACTORS MA
WHERE M.MOVIE_ID = MA.MOVIE_ID
AND a.ACTOR_ID = MA.ACTOR_ID
AND M.TITLE = '기생충';

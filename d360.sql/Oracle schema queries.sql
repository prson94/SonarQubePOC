/* TABLE */
select  O.OBJECT_ID as "ID",
        101 as "FusionAttributeTypeID",
        T.TABLE_NAME as "Name",
        T.AVG_ROW_LEN as "AverageRowLength",
        CASE T.BACKED_UP
            WHEN 'N' THEN 0
            ELSE 1
        END as "BackupEnabled",
        T.PCT_FREE as "PercentFree",
        T.LAST_ANALYZED as "LastAnalyzed",
        CASE T.LOGGING
            WHEN 'NO' THEN 0
            ELSE 1
        END as "LoggingEnabled",
        CASE T.PARTITIONED 
            WHEN 'NO' THEN 0
            ELSE 1
        END as "Partitioned"
from    DBA_TABLES T
        INNER JOIN DBA_OBJECTS O ON T.TABLE_NAME = O.OBJECT_NAME AND O.OBJECT_TYPE = 'TABLE'
where   T.TEMPORARY = 'N' AND T.OWNER = 'XXGAM';

select OBJECT_TYPE from DBA_OBJECTS group by OBJECT_TYPE;

/* PRIMARY KEY */
select      OC.OBJECT_ID as "ID",
            103 as "FusionAttributeTypeID",
            C.CONSTRAINT_NAME as "Name",
            CASE C.STATUS
                WHEN 'ENABLED' THEN 1
                ELSE 0
            END as "Enabled"
from        DBA_CONSTRAINTS C
            INNER JOIN DBA_OBJECTS OC ON C.CONSTRAINT_NAME = OC.OBJECT_NAME AND OC.OBJECT_TYPE = 'INDEX'
where       C.OWNER = 'XXGAM'
            and C.CONSTRAINT_TYPE = 'P';

/* FOREIGN KEY */
select      OT.OBJECT_ID || '.' || ora_hash(C.CONSTRAINT_NAME) as "ID",
            104 as "FusionAttributeTypeID",
            C.CONSTRAINT_NAME as "Name",
            C.DELETE_RULE as "DeleteRule",
            CASE C.STATUS
                WHEN 'ENABLED' THEN 1
                ELSE 0
            END as "Enabled"
from        DBA_CONSTRAINTS C
            LEFT JOIN DBA_OBJECTS OT ON C.TABLE_NAME = OT.OBJECT_NAME AND OT.OBJECT_TYPE = 'TABLE'
where       C.OWNER = 'XXGAM'
            and C.CONSTRAINT_TYPE = 'R';

/* COLUMN */
select  O.OBJECT_ID || '.' || C.INTERNAL_COLUMN_ID as "ID",
        O.OBJECT_ID as "ParentID",
        102 as "FusionAttributeTypeID",
        C.COLUMN_NAME as "Name",
        C.DATA_TYPE as "DataType",
        C.DATA_LENGTH as "Length",
        C.DATA_PRECISION as "Precision",
        C.DATA_SCALE as "Scale",
        CASE C.NULLABLE 
            WHEN 'Y' THEN 1
            ELSE 0
        END as "Nullable",
        C.DATA_DEFAULT as "Default",
        C.NUM_DISTINCT as "DistinctValues",
        C.NUM_NULLS as "NullValueCount",
        C.AVG_COL_LEN as "AverageLength"
from    DBA_TAB_COLS C
        INNER JOIN DBA_OBJECTS O ON C.TABLE_NAME = O.OBJECT_NAME AND O.OBJECT_TYPE = 'TABLE'
where   C.OWNER = 'XXGAM';

/* DATABASE LINK */
select  'DBLINK.' || ora_hash(DB_LINK) as "ID",
        113 as "FusionAttributeTypeID",
        DB_LINK as "Name",
        USERNAME as "Username",
        HOST as "ConnectionString"
from    DBA_DB_LINKS L
where   L.OWNER = 'XXGAM';

/* INDEX */
select  O.OBJECT_ID as "ID",
        105 as "FusionAttributeTypeID",
        I.INDEX_NAME as "Name",
        CASE I.UNIQUENESS
            WHEN 'UNIQUE' THEN 1
            ELSE 0
        END as "Unique",
        I.DISTINCT_KEYS as "KeyCount",
        I.NUM_ROWS as "RowCount",
        I.PCT_FREE as "PercentFree",
        CASE I.STATUS
            WHEN 'VALID' THEN 1
            ELSE 0
        END as "Valid",
        I.LAST_ANALYZED as "LastAnalyzed",
        CASE I.PARTITIONED 
            WHEN 'NO' THEN 0
            ELSE 1
        END as "Partitioned"
from    DBA_INDEXES I
        INNER JOIN DBA_OBJECTS O ON I.INDEX_NAME = O.OBJECT_NAME AND O.OBJECT_TYPE = 'INDEX'
where   INDEX_NAME NOT IN (SELECT CONSTRAINT_NAME FROM DBA_CONSTRAINTS WHERE OWNER = 'XXGAM')
        AND I.OWNER = 'XXGAM';

/* TRIGGER */
select  O.OBJECT_ID as "ID",
        106 as "FusionAttributeTypeID",
        T.TRIGGER_NAME as "Name",
        T.TRIGGER_TYPE as "SubType",
        T.TRIGGERING_EVENT as "Event",
        CASE T.STATUS
            WHEN 'ENABLED' THEN 1
            ELSE 0
        END as "Enabled",
        T.TRIGGER_BODY as "Body",
        CASE T.BEFORE_STATEMENT
            WHEN 'NO' THEN 0
            ELSE 1
        END as "BeforeStatement",
        CASE T.BEFORE_ROW
            WHEN 'NO' THEN 0
            ELSE 1
        END as "BeforeRow",
        CASE T.AFTER_STATEMENT
            WHEN 'NO' THEN 0
            ELSE 1
        END as "AfterStatement",
        CASE T.AFTER_ROW
            WHEN 'NO' THEN 0
            ELSE 1
        END as "AfterRow",
        CASE T.INSTEAD_OF_ROW
            WHEN 'NO' THEN 0
            ELSE 1
        END as "InsteadOfRow"
from    DBA_TRIGGERS T
        INNER JOIN DBA_OBJECTS O ON T.TRIGGER_NAME = O.OBJECT_NAME AND O.OBJECT_TYPE = 'TRIGGER'
where   T.OWNER = 'XXGAM';

/* VIEW */
select  O.OBJECT_ID as "ID",
        107 as "FusionAttributeTypeID",
        V.VIEW_NAME as "Name",
        V.TEXT as "Body",
        CASE V.EDITIONING_VIEW
            WHEN 'N' THEN 0
            ELSE 1
        END as "Editioned",
        CASE V.READ_ONLY
            WHEN 'N' THEN 0
            ELSE 1
        END as "ReadOnly"
from    DBA_VIEWS V
        INNER JOIN DBA_OBJECTS O ON V.VIEW_NAME = O.OBJECT_NAME AND O.OBJECT_TYPE = 'VIEW'
where   V.OWNER = 'XXGAM';

/* MATERIALIZED VIEW */
select  O.OBJECT_ID as "ID",
        108 as "FusionAttributeTypeID",
        V.MVIEW_NAME as "Name",
        V.QUERY as "Body",
        V.REFRESH_MODE as "RefreshMode",
        V.REFRESH_METHOD as "RefreshMethod",
        V.LAST_REFRESH_TYPE as "LastRefreshType",
        V.LAST_REFRESH_DATE as "LastRefreshOn"
from    DBA_MVIEWS V
        INNER JOIN DBA_OBJECTS O ON V.MVIEW_NAME = O.OBJECT_NAME AND O.OBJECT_TYPE = 'MATERIALIZED VIEW'
where   V.OWNER = 'XXGAM';

/* PERMISSION */
select  O.OBJECT_ID || '.' || U.USER_ID || '.' || SUBSTR(P.PRIVILEGE, 1, 3) as "ID",
        110 as "FusionAttributeTypeID",
        O.OBJECT_ID as "ParentID",
        P.GRANTEE as "Name",
        P.GRANTOR as "Grantor",
        P.PRIVILEGE as "Permission",
        U.ACCOUNT_STATUS as "Status",
        U.LOCK_DATE as "DateLocked",
        U.EXPIRY_DATE as "DateExpired"
from    DBA_TAB_PRIVS P
        INNER JOIN DBA_USERS U ON P.GRANTEE = U.USERNAME
        INNER JOIN DBA_OBJECTS O ON P.TABLE_NAME = O.OBJECT_NAME AND O.OBJECT_TYPE = 'TABLE'
where   P.OWNER = 'XXGAM';

/* SEQUENCE */
select  O.OBJECT_ID as "ID",
        109 as "FusionAttributeTypeID",
        S.SEQUENCE_NAME as "Name",
        S.MIN_VALUE as "Minimum",
        S.MAX_VALUE as "Maximum",
        S.INCREMENT_BY as "Increment"
from    DBA_SEQUENCES S
        INNER JOIN DBA_OBJECTS O ON S.SEQUENCE_NAME = O.OBJECT_NAME AND O.OBJECT_TYPE = 'SEQUENCE'
where   S.SEQUENCE_OWNER = 'XXGAM';

/* PROCEDURE */
select  OBJECT_ID || CASE OBJECT_TYPE WHEN 'PACKAGE' THEN '.' || SUBPROGRAM_ID ELSE '' END as "ID",
        111 as "FusionAttributeTypeID",
        OBJECT_NAME || CASE WHEN PROCEDURE_NAME IS NOT NULL THEN '.' || PROCEDURE_NAME ELSE '' END as "Name",
        CASE OBJECT_TYPE
            WHEN 'PROCEDURE' THEN 'Stand-alone Procedure' 
            ELSE 'Package Procedure'
        END as "Subtype",
        CASE 
            WHEN OBJECT_TYPE = 'PACKAGE' AND SUBPROGRAM_ID = 0 THEN 0
            ELSE 1
        END as "Valid"
from    DBA_PROCEDURES
where   OBJECT_TYPE IN ('PROCEDURE', 'PACKAGE') 
        and OWNER = 'XXGAM';

/* FUNCTION */
select  OBJECT_ID as "ID",
        112 as "FusionAttributeTypeID",
        OBJECT_NAME as "Name",
        CASE 
            WHEN OBJECT_TYPE = 'PACKAGE' AND SUBPROGRAM_ID = 0 THEN 0
            ELSE 1
        END as "Valid"
from    DBA_PROCEDURES
where   OBJECT_TYPE = 'FUNCTION' 
        and OWNER = 'XXGAM';



select OBJECT_TYPE from DBA_PROCEDURES group by OBJECT_TYPE



/* Relationships */

        --MV
select  OM.OBJECT_ID as "Side1ID",
        OV.OBJECT_ID as "Side2ID",
        M.* 
from    DBA_MVIEW_DETAIL_RELATIONS M
        INNER JOIN DBA_OBJECTS OM ON M.MVIEW_NAME = OM.OBJECT_NAME AND OM.OBJECT_TYPE = 'MATERIALIZED VIEW'
        INNER JOIN DBA_OBJECTS OV ON M.DETAILOBJ_NAME = OV.OBJECT_NAME AND OV.OBJECT_TYPE = 'VIEW'
where   M.OWNER = 'XXGAM';
UNION   --PK
select  OC.OBJECT_ID as "Side1ID",
        OT.OBJECT_ID as "Side2ID"
from    DBA_CONSTRAINTS C
        INNER JOIN DBA_OBJECTS OC ON C.CONSTRAINT_NAME = OC.OBJECT_NAME AND OC.OBJECT_TYPE = 'INDEX'
        INNER JOIN DBA_OBJECTS OT ON C.TABLE_NAME = OT.OBJECT_NAME AND OT.OBJECT_TYPE = 'TABLE'
where   C.OWNER = 'XXGAM'
        and C.CONSTRAINT_TYPE = 'P';
UNION   --FK
select  OT.OBJECT_ID || '.' || ora_hash(C.CONSTRAINT_NAME) as "Side1ID",
        OT.OBJECT_ID as "Side2ID"
from    DBA_CONSTRAINTS C
        INNER JOIN DBA_OBJECTS OT ON C.TABLE_NAME = OT.OBJECT_NAME AND OT.OBJECT_TYPE = 'TABLE'
where   C.OWNER = 'XXGAM'
        and C.CONSTRAINT_TYPE = 'R';
UNION   -- IX
select  OI.OBJECT_ID as "Side1ID",
        OT.OBJECT_ID as "Side2ID"
from    DBA_INDEXES I
        INNER JOIN DBA_OBJECTS OI ON I.INDEX_NAME = OI.OBJECT_NAME AND OI.OBJECT_TYPE = 'INDEX'
        INNER JOIN DBA_OBJECTS OT ON I.TABLE_NAME = OT.OBJECT_NAME AND OT.OBJECT_TYPE = 'TABLE'
where   I.OWNER = 'XXGAM';
UNION   -- Index/column link
select  OI.OBJECT_ID as "Side1ID",
        OT.OBJECT_ID || '.' || C.INTERNAL_COLUMN_ID as "Side2ID"
from    DBA_IND_COLUMNS I
        INNER JOIN DBA_OBJECTS OI ON I.INDEX_NAME = OI.OBJECT_NAME and OI.OBJECT_TYPE = 'INDEX'
        INNER JOIN DBA_TAB_COLS C ON C.TABLE_NAME = I.TABLE_NAME and C.COLUMN_NAME = I.COLUMN_NAME
        INNER JOIN DBA_OBJECTS OT ON C.TABLE_NAME = OT.OBJECT_NAME and OT.OBJECT_TYPE = 'TABLE'
where   I.INDEX_OWNER = 'XXGAM';
UNION   -- Trigger/table link
select  OI.OBJECT_ID as "Side1ID",
        OT.OBJECT_ID as "Side2ID"
from    DBA_TRIGGERS I
        INNER JOIN DBA_OBJECTS OI ON I.TRIGGER_NAME = OI.OBJECT_NAME and OI.OBJECT_TYPE = 'TRIGGER'
        INNER JOIN DBA_OBJECTS OT ON I.TABLE_NAME = OT.OBJECT_NAME and OT.OBJECT_TYPE = 'TABLE'
where   I.OWNER = 'XXGAM';
UNION   -- Trigger/column link
select  OI.OBJECT_ID as "Side1ID",
        OT.OBJECT_ID || '.' || C.INTERNAL_COLUMN_ID as "Side2ID"
from    DBA_TRIGGER_COLS I
        INNER JOIN DBA_OBJECTS OI ON I.TRIGGER_NAME = OI.OBJECT_NAME and OI.OBJECT_TYPE = 'TRIGGER'
        INNER JOIN DBA_TAB_COLS C ON C.TABLE_NAME = I.TABLE_NAME and C.COLUMN_NAME = I.COLUMN_NAME
        INNER JOIN DBA_OBJECTS OT ON C.TABLE_NAME = OT.OBJECT_NAME and OT.OBJECT_TYPE = 'TABLE'
where   I.TRIGGER_OWNER = 'XXGAM';
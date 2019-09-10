SET version=1.0.46

CALL test.cmd %version%
CALL release.cmd %version%
CALL publish.cmd %version%
CALL pack.cmd %version%
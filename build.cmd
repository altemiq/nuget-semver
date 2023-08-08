SET version=2.1.5

CALL test.cmd
CALL release.cmd %version%
CALL pack.cmd %version%
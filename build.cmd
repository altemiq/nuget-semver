SET version=2.1.6

CALL test.cmd
CALL release.cmd %version%
CALL pack.cmd %version%
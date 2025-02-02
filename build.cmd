SET version=2.1.8

CALL test.cmd
CALL release.cmd %version%
CALL pack.cmd %version%
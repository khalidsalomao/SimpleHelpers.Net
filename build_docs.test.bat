rd "mkdocs_site\" /s /q
md "mkdocs_site"
COPY "README.md" "mkdocs_site\index.md" /y
XCOPY "docs\*" "mkdocs_site\docs\" /y /i
mkdocs serve

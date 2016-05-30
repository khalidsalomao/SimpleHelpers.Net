How to build docs site
======================

- Built with [MkDocs](http://www.mkdocs.org/).
- Theme: [Material](http://squidfunk.github.io/mkdocs-material/getting-started/)


Build steps
-----------

1. Make sure MkDocs is installed, using python package manager - pip:

    ```
    python --version
    pip install mkdocs pymdown-extensions pygments mkdocs-material --upgrade
    npm install
    ```

2. Test the docs site:
    '''
    npm run test:docs
    '''

3. Build and deploy on gh-pages:
    '''
    npm run deploy:docs
    '''


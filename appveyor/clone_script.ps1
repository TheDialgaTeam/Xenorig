git clone -q --depth=1 --recursive --branch="${env:APPVEYOR_REPO_BRANCH}" "https://github.com/${env:APPVEYOR_REPO_NAME}.git" "${env:APPVEYOR_BUILD_FOLDER}"
git checkout -qf "${env:APPVEYOR_REPO_COMMIT}"

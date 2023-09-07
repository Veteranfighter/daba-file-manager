<?php

    /*
        DISPLAY ADDITIONAL VERBOSITY INFORMATION
        [ !!! DISABLE IN PRODUCTION USE !!! ]
    */
    if(true){
        ini_set('display_errors', 1);
        ini_set('display_startup_errors', 1);
        error_reporting(E_ALL);
    }

    if($_SERVER['REQUEST_METHOD'] !== 'GET' && $_SERVER['REQUEST_METHOD'] !== 'POST'){
        http_response_code(400);
        die();
    }

    function print_header() {
        $result = '';
        $result .= '<!doctype html>';
        $result .= '<html lang="de">';
        $result .= '<head>';
        $result .= '<meta charset="utf-8">';
        $result .= '<meta name="viewport" content="width=device-width, initial-scale=1">';
        $result .= '<title>DaBa File-Manager API</title>';
        $result .= '<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.1/dist/css/bootstrap.min.css" rel="stylesheet">';
        $result .= '<link href="./prism@1.29.0.min.css" rel="stylesheet">';
        $result .= '<link rel="preconnect" href="https://fonts.googleapis.com">';
        $result .= '<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>';
        $result .= '<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500&display=swap" rel="stylesheet">';
        $result .= '</head>';
        $result .= '<body data-bs-theme="dark">';
        $result .= '';
        $result .= '<nav class="navbar navbar-expand-lg bg-body-tertiary">';
        $result .= '<div class="container-fluid">';
        $result .= '<a class="navbar-brand" style="font-family: Inter" href="./">DaBa File-Manager API</a>';
        $result .= '</div>';
        $result .= '</nav>';
        $result .= '<div class="container">';
        $result .= '<pre class="my-3 p-3 border rounded bg-body-tertiary"><code class="language-json">';
        echo $result;
    }

    function print_footer() {
        $result = '';
        $result .= '</code>';
        $result .= '</pre>';
        $result .= '</div>';
        $result .= '<script src="./prism@1.29.0.min.js"></script>';
        $result .= '</body>';
        $result .= '</html>';
        echo $result;
    }

    if(isset($_GET['gui'])){
        if(isset($_GET['gui']) === true){
            $DABA_API_GUI = true;
        }
    }

    if($DABA_API_GUI){
        print_header();
    }

    if(file_exists("config.json")){
        if(is_readable("config.json")){
            try {
                $DABA_API_DB_CONFIG = file_get_contents("config.json");
                try {
                    $DABA_API_DB_CONFIG = json_decode($DABA_API_DB_CONFIG, true);
                } catch(Exception $e) {
                    error_log($e->getMessage());
                    exit('Error decoding database config');
                }
            } catch(Exception $e) {
                error_log($e->getMessage());
                exit('Error reading database config');
            }
        }
    }

    mysqli_report(MYSQLI_REPORT_ERROR | MYSQLI_REPORT_STRICT);
    try {
        $mysqli = new mysqli($DABA_API_DB_CONFIG["hostname"], $DABA_API_DB_CONFIG["username"], $DABA_API_DB_CONFIG["password"], $DABA_API_DB_CONFIG["database"], $DABA_API_DB_CONFIG["hostport"]);
        $mysqli->set_charset("utf8mb4");
    } catch(Exception $e) {
        error_log($e->getMessage());
        exit('Error connecting to database');
    }

    $sql = "SELECT * FROM storage";
    try {
        $result = $mysqli->query($sql);
    } catch(Exception $e) {
        error_log($e->getMessage());
        exit('Database Error: '.$e->getMessage());
    }
    
    if ($result->num_rows > 0) {
        while($row = $result->fetch_assoc()) {
            echo(json_encode($row, JSON_PRETTY_PRINT).PHP_EOL);
        }
    } else {
        echo "0 results";
    }
    $mysqli->close();

    if($DABA_API_GUI){
        print_footer();
    }

?>
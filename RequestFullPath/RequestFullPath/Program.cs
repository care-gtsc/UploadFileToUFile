using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace RequestFullPath
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();
        static HttpResponseMessage response;
        private static string path, url_session, url_chunk, url_finalise, csrf_test_name, chunk_index, fuid, total_chunks, session_id;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Default;
            //path = "C:\Users\Care\Downloads\SIEM.docx";
            url_session = "https://up.ufile.io/v1/upload/create_session";
            url_chunk = "https://up.ufile.io/v1/upload/chunk";
            url_finalise = "https://up.ufile.io/v1/upload/finalise";

            Console.WriteLine("Khởi tạo...");
            (csrf_test_name, session_id) = await GetHiddenKey();

            Console.WriteLine("Nhập đường dẫn file mà bạn muốn upload: ");
            path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path))
            {
                Console.WriteLine("Đã nhận được file bạn yêu cầu!");
            }
            else
            {
                Console.WriteLine("Ồ, có lỗi với đường dẫn của bạn, xin thử lại");
                return;
            }

            //create_session
            Console.WriteLine("Bắt đầu tạo cổng kết nối...");
            fuid = await CreateSession(url_session, csrf_test_name, path);
            if (string.IsNullOrEmpty(fuid)) 
            {
                Console.WriteLine("Không thể tạo kết nối, vui lòng thử lại!");
                return;
            }

            //chunk
            chunk_index = "1";  //thử nghiệm chỉ tải 1 file lên
            Console.WriteLine("Đã tạo cổng kết nối, bắt đầu tải file lên hệ thống...");
            bool check_upload = await RequestChunk(url_chunk, chunk_index, fuid, path);
            if (check_upload)
            {
                Console.WriteLine("Tải file lên hệ thống thành công. Hoàn tất quá trình...");
            }
            else
            {
                Console.WriteLine("Có lỗi khi tải file lên hệ thống. Vui lòng thử lại!");
                return;
            }

            //finalise
            total_chunks = "1";
            var check_finalise = await RequestFinalise(url_finalise, csrf_test_name, fuid, path, total_chunks, session_id);

            if (!string.IsNullOrEmpty(check_finalise))
            {
                string result = check_finalise.Replace("\\", "");
                Console.WriteLine("Tải file thành công!");
                Console.WriteLine($"Đường dẫn của bạn là: {result}");
            }
            else
            {   
                Console.WriteLine("!!!");
                return;
            }
        }

        private static async Task<(string, string)> GetHiddenKey()
        {
            string csrf_test_name, session_id;
            response = await client.GetAsync("https://ufile.io/");
            var responseBut = await response.Content.ReadAsStringAsync();
            csrf_test_name = responseBut.Substring(40276, 79).Split('"')[5];
            session_id = responseBut.Substring(40041, 80).Split('"')[5];
            return (csrf_test_name, session_id);
        }
        
        private static async Task<string> CreateSession(string url_session, string csrf_test_name, string path)
        {
            MultipartFormDataContent session_content = new MultipartFormDataContent() {
                { new StringContent(csrf_test_name), "csrf_test_name" },
                { new StringContent(new FileInfo(path).Length.ToString()), "file_size" },
            };
            response = await client.PostAsync(url_session, session_content);
            return response.IsSuccessStatusCode ? response.Content.ReadAsStringAsync().Result.Split('"')[3] : null;
        }

        private static async Task<bool> RequestChunk(string url_chunk, string chunk_index, string fuid, string path)
        {
            string boundary = "----WebKitFormBoundaryg2vqSXsecIMvSCtB";
            //var files = File.ReadAllBytes(path);

            FileStream file = new FileStream(path, FileMode.Open);
            StreamContent streamCt = new StreamContent(file);
            //streamCt.Headers.Add("Content-Disposition", "form-data");
            streamCt.Headers.Add("Content-Type", "application/octet-stream");

            MultipartFormDataContent chunk_content = new MultipartFormDataContent(boundary) {
                { new StringContent(fuid), "fuid" },
                { new StringContent(chunk_index), "chunk_index" },
            };
            chunk_content.Add(streamCt, "file", Path.GetFileName(path));

            response = await client.PostAsync(url_chunk, chunk_content);
            return response.IsSuccessStatusCode;
        }

        private static async Task<string> RequestFinalise(string url_finalise, string csrf_test_name, string fuid, string path, string total_chunks, string session_id)
        {
            MultipartFormDataContent finalise_content = new MultipartFormDataContent() {
                    { new StringContent(csrf_test_name), "csrf_test_name" },
                    { new StringContent(fuid), "fuid" },
                    { new StringContent(Path.GetFileName(path)), "file_name" },
                    { new StringContent(Path.GetExtension(path).Replace(".","")), "file_type" },
                    { new StringContent("1"), "total_chunks" },
                    { new StringContent(session_id), "session_id" },
                };
            response = await client.PostAsync(url_finalise, finalise_content);
            return response.IsSuccessStatusCode ? response.Content.ReadAsStringAsync().Result.Split('"')[5] : null;
        }
    }
}
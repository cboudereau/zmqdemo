module String

let decode = System.Text.UTF8Encoding.UTF8.GetString
let encode s = System.Text.UTF8Encoding.UTF8.GetBytes(s:string)
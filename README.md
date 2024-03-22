# 如何提取翻译文件:
 * 从[CP] Fishing Made Easy Suite\content.json文件中，找到ConfigSchema字段，
 * 复制它的{}里的的值（包括括号）到本目录的content.json里，大约在1000多行左右，删掉最后的一个逗号（应该是作者不小心留下的）
 * 运行parse.py，会在i18n自动生成最新zh.json翻译文件
 * 通过软件翻译zh.json里冒号:后面的文本

 

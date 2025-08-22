/*
 * Click nbfs://nbhost/SystemFileSystem/Templates/Licenses/license-default.txt to change this license
 * Click nbfs://nbhost/SystemFileSystem/Templates/Classes/Class.java to edit this template
 */
package View;


/**
 *
 * @author QH
 */
public class ViewCourse {
      
    
    // Hiển thị menu
    public void printMenu() {
        System.out.println(
            "COURSE MANAGEMENT SYSTEM\n" +
            "1. A list of all available courses in the system\n" +
            "2. Search and display information of  a course by course id\n" +
            "3. Record/Add information of course\n" +
            "4. Sort all courses by number of credit as ascending\n" +
            "5. Update information of a specific course (by course id)\n" +
            "0. Exit"
        );
    }


   // Hiển thị thông điệp
    public void displayMess(String mess){
        System.out.println(mess);
    }
    
}

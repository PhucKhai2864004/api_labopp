/*
 * Click nbfs://nbhost/SystemFileSystem/Templates/Licenses/license-default.txt to change this license
 */

package demo_CourseManager;

import Control.CourseController;
import View.InputData;
import View.ViewCourse;
import java.util.Scanner;

/**
 *
 * @author QH
 */
public class App_Course_Manager {

    public static void main(String[] args) {
         Scanner sc = new Scanner(System.in);
         ViewCourse view = new ViewCourse();
         InputData inp = new InputData(sc);
         Control.CourseController controller = new CourseController(inp);

        int choose;
        while(true){
        // hiển thị menu
        view.printMenu();
        // cho người dùng chọn chức năng 1-6
                       
        choose = inp.inputInteger("Select your choice: ", "[0-5]{1}");
            switch (choose) {
                case 1:
                    controller.displayListOfCourse();                    
                    break;
                case 2:
                     controller.displayCourse();                    
                    break;
                case 3:
                     controller.addCourse();                    
                    break;
                case 4:
                     controller.displayListOfCourseSorted();
                    break;
                case 5:
                     controller.updateCourse();                    
                    break;
                default:
                    System.out.println("Exit program");
                    System.exit(0);
            }
        }
    }
}

package mathUtil;

import static org.junit.Assert.*;

import org.junit.Before;
import org.junit.Test;

public class UDGraphTest {

	@Before
	public void setUp() throws Exception {
	}

	@Test
	public void test() {
		int[] end ={73,74,75,76,77,78,79,80,81};
		int[][] graph=new int[81][81];
		for(int i=0;i<81;i++){
			for(int j=0;j<81;j++){
				if(j==i+9||j==i-9||j==i+1||j==i-1){
					graph[i][j]=1;
				}
			}
		}
		for(int i=1;i<9;i++){
			graph[i*9][i*9-1]=0;
			graph[i*9-1][i*9]=0;
		}
		for(int i=72;i<81;i++){
			graph[i][i-9]=0;
			graph[i-9][i]=0;
		}
		for(int i=0;i<81;i++){
			for(int j=0;j<81;j++){
				System.out.print(graph[i][j]);
			}
			System.out.println();
		}	
		UDGraph test=new UDGraph(graph);
		assertTrue(test.startTest(0, end)==false);
		graph[63][72]=1;
		graph[72][63]=1;
		test=new UDGraph(graph);
		assertTrue(test.startTest(0, end));
	}

}

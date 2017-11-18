<template>
  <v-layout @mouseover='mouseOver' @mouseleave='mouseOut' align-center>
    <v-flex xs2>
      <v-btn icon small xs flat @click.native='visible=!visible' color='grey'>
      <v-icon dark>{{ visible ? "visibility" : "visibility_off" }}</v-icon>
      </v-btn>
    </v-flex>
    <v-flex xs10>
      <div class='subheading'>{{ layer.name }}</div>
      <div class="caption grey--text"> Object count: {{layer.objectCount }} </div>
    </v-flex>
    <v-flex>
      <v-icon dark :style='{ color: layerColor.hex }'>fiber_manual_record</v-icon>
    </v-flex>
  </v-layout>
</template>

<script>

  export default {
    name: '',
    props: {
      layer: Object
    },
    components: {},
    computed: {
      layerColor() {
        if( this.layer.properties && this.layer.properties.color )
          return this.layer.properties.color
        return { hex: '#00FF00', alpha: 1 }
      }
    },
    data() {
      return {
        visible: true
      }
    },
    methods: {
      mouseOver() {
        // console.log('in')
      },
      mouseOut() {
        // console.log('out')
      }
    },
    mounted() {
    }
  }
</script>

<style lang='scss'>
    
</style>